using OpenTK.Audio.OpenAL;

var devices = ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier).ToList();
Console.WriteLine($"Devices: {string.Join(", ", devices)}");

// Get the default device, then go though all devices and select the AL soft device if it exists.
var deviceName = ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);
foreach (var d in devices.Where(d => d.Contains("OpenAL Soft")))
{
    deviceName = d;
    break;
}

var device = ALC.OpenDevice(deviceName);
var context = ALC.CreateContext(device, (int[])null!);
ALC.MakeContextCurrent(context);

CheckAlError("Start");

ALC.GetInteger(device, AlcGetInteger.MajorVersion, 1, out var alcMajorVersion);
ALC.GetInteger(device, AlcGetInteger.MinorVersion, 1, out var alcMinorVersion);
var alcExts = ALC.GetString(device, AlcGetString.Extensions);

var attrs = ALC.GetContextAttributes(device);
Console.WriteLine($"Attributes: {attrs}");

var exts = AL.Get(ALGetString.Extensions);
var rend = AL.Get(ALGetString.Renderer);
var vend = AL.Get(ALGetString.Vendor);
var vers = AL.Get(ALGetString.Version);

Console.WriteLine(
    $"Vendor: {vend}, \nVersion: {vers}, \nRenderer: {rend}, \nExtensions: {exts}, \nALC Version: {alcMajorVersion}.{alcMinorVersion}, \nALC Extensions: {alcExts}");

Console.WriteLine("Available devices: ");
var list = ALC.EnumerateAll.GetStringList(GetEnumerateAllContextStringList.AllDevicesSpecifier);
foreach (var item in list)
{
    Console.WriteLine("  " + item);
}

var auxSlot = 0;
if (ALC.EFX.IsExtensionPresent(device))
{
    Console.WriteLine("EFX extension is present!!");
    ALC.EFX.GenEffect(out int effect);
    ALC.EFX.Effect(effect, EffectInteger.EffectType, (int)EffectType.Reverb);
    ALC.EFX.GenAuxiliaryEffectSlot(out auxSlot);
    ALC.EFX.AuxiliaryEffectSlot(auxSlot, EffectSlotInteger.Effect, effect);
}

AL.GenBuffer(out int alBuffer);

// Генерация синусоидальных сигналов для левого и правого каналов
var sine0 = new short[44100]; // Левый канал
FillSine(sine0, 2000, 44100);

var sine1 = new short[44100]; // Правый канал
FillSine(sine1, 500, 44100);

// Создание стерео-буфера
var data = new short[44100 * 2]; // 2 канала * 44100 семплов
for (int i = 0, j = 0; i < 44100; i++)
{
    data[j++] = sine0[i]; // Левый канал
    data[j++] = sine1[i]; // Правый канал
}
FillRnd(data);
// Передача данных в OpenAL
AL.BufferData(alBuffer, ALFormat.Stereo16, ref data[0], data.Length * sizeof(short), 44100);

// Настройка источника и воспроизведение
AL.Listener(ALListenerf.Gain, 0.2f);

AL.GenSource(out int alSource);
AL.Source(alSource, ALSourcef.Gain, 1f);
AL.SourceQueueBuffer(alSource, alBuffer);
AL.SourceQueueBuffer(alSource, alBuffer);
AL.SourceQueueBuffer(alSource, alBuffer);
AL.SourceQueueBuffer(alSource, alBuffer);
AL.SourceQueueBuffer(alSource, alBuffer);
AL.SourceQueueBuffer(alSource, alBuffer);
AL.SourceQueueBuffer(alSource, alBuffer);
AL.SourceQueueBuffer(alSource, alBuffer);
AL.SourceQueueBuffer(alSource, alBuffer);

// Проверка на наличие EFX и привязка эффекта (если нужно)
if (ALC.EFX.IsExtensionPresent(device))
{
    ALC.EFX.Source(alSource, EFXSourceInteger3.AuxiliarySendFilter, auxSlot, 0, 0);
}
Console.WriteLine("Before Playing: " + AL.GetErrorString(AL.GetError()));
AL.SourcePlay(alSource);

await Task.Delay(100);

while ((ALSourceState)AL.GetSource(alSource, ALGetSourcei.SourceState) == ALSourceState.Playing)
{
    if (ALC.DeviceClock.IsExtensionPresent(device))
    {
        long[] clockLatency = new long[2];
        ALC.DeviceClock.GetInteger(device, GetInteger64.DeviceClock, 1, clockLatency);
        Console.WriteLine("Clock: " + clockLatency[0] + ", Latency: " + clockLatency[1]);
        CheckAlError(" ");
    }
    
    if (AL.SourceLatency.IsExtensionPresent())
    {
        AL.SourceLatency.GetSource(alSource, SourceLatencyVector2d.SecOffsetLatency, out var values);
        AL.SourceLatency.GetSource(alSource, SourceLatencyVector2i.SampleOffsetLatency, out var values1,
            out var values2, out var values3);
        Console.WriteLine("Source latency: " + values);
        Console.WriteLine($"Source latency 2: {values1}, {values2}; {values3}");
        CheckAlError(" ");
    }

    await Task.Delay(50);
}

AL.GetSource(alSource, ALGetSourcei.BuffersProcessed, out var processed);
if (processed > 0)
{
    var buff = AL.SourceUnqueueBuffer(alSource);
    
}
AL.GetSource(alSource, ALGetSourcei.BuffersProcessed, out processed);

AL.SourceStop(alSource);
return;

void CheckAlError(string str)
{
    var error = AL.GetError();
    if (error != ALError.NoError)
    {
        Console.WriteLine($"ALError at '{str}': {AL.GetErrorString(error)}");
    }
}

void FillSine(short[] buffer, float frequency, float sampleRate, float sound = 1f)
{
    for (var i = 0; i < buffer.Length; i++)
    {
        buffer[i] = (short)(MathF.Sin((i * frequency * MathF.PI * 2) / sampleRate) * short.MaxValue * sound);
    }
}

void FillSineBytes(byte[] buffer, float frequency, float sampleRate, float sound = 1f)
{
    for (var i = 0; i < buffer.Length; i++)
    {
        buffer[i] = (byte)(MathF.Sin((i * frequency * MathF.PI * 2) / sampleRate) * byte.MaxValue * sound);
    }
}

void FillRnd(short[] buffer)
{
    var rnd = new Random();
    for (var i = 0; i < buffer.Length; i++)
    {
        var bytes = new byte[2];
        rnd.NextBytes(bytes);
        buffer[i] = BitConverter.ToInt16(bytes);
    }
}

short[] Add(short[] left, short[] right)
{
    var res = new short[Math.Max(left.Length, right.Length)];
    for (var i = 0; i < res.Length; i++)
    {
        if (i < left.Length)
            res[i] += left[i];
        if (i < right.Length)
            res[i] += right[i];
    }

    return res;
}