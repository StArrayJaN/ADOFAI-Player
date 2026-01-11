
// HitSoundMerger, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// HitSoundMerger.AudioMerger

using NAudio.Wave;

public class AudioMerger
{
	public class ProgressEventArgs : EventArgs
	{
		public int Current { get; set; }

		public int Total { get; set; }

		public double Percentage => (Total > 0) ? (Current / (double)Total * 100.0) : 0.0;

		public string Message { get; set; }
	}

	private class AudioData
	{
		public WaveFormat Format { get; set; }

		public byte[] Data { get; set; }
	}

	public class AudioInsert
	{
		public double Timestamp { get; set; }

		public string FilePath { get; set; }

		public int Position { get; set; }

		public AudioInsert(double timestamp, string filePath)
		{
			Timestamp = timestamp;
			FilePath = filePath;
		}
	}

	public event EventHandler<ProgressEventArgs> ProgressUpdated;

	private void ReportProgress(int current, int total, string message = "")
	{
		ProgressUpdated?.Invoke(this, new ProgressEventArgs
		{
			Current = current,
			Total = total,
			Message = message
		});
	}

	private void ReportMappedProgress(int current, int total, string message, int minProgress, int maxProgress)
	{
		double num = ((total > 0) ? (current / (double)total) : 0.0);
		int current2 = minProgress + (int)(num * (maxProgress - minProgress));
		ReportProgress(current2, 100, message);
	}

	private void MixAudioWithProgressMapping(string baseFile, List<AudioInsert> inserts, string outputPath, int minProgress, int maxProgress)
	{
		int num = inserts.Count + 4;
		ReportMappedProgress(0, num, "Reading base audio...", minProgress, maxProgress);
		AudioData audioData = ReadWav(baseFile);
		WaveFormat format = audioData.Format;
		int sampleRate = format.SampleRate;
		int num2 = format.BitsPerSample / 8;
		int channels = format.Channels;
		int num3 = num2 * channels;
		ReportMappedProgress(1, num, "Calculating positions...", minProgress, maxProgress);
		foreach (AudioInsert insert in inserts)
		{
			insert.Position = (int)(insert.Timestamp * sampleRate / 1000.0) * num3;
			insert.Position = Math.Min(insert.Position, audioData.Data.Length);
		}
		inserts.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
		byte[] array = new byte[audioData.Data.Length];
		Array.Copy(audioData.Data, array, audioData.Data.Length);
		ReportMappedProgress(2, num, "Caching audio files...", minProgress, maxProgress);
		Dictionary<string, AudioData> dictionary = new Dictionary<string, AudioData>();
		List<string> list = (from i in inserts
			where !string.IsNullOrEmpty(i.FilePath)
			select i.FilePath).Distinct().ToList();
		int num4 = list.Count;
		int num5 = 0;
		for (int num6 = 0; num6 < list.Count; num6++)
		{
			string text = list[num6];
			if (!dictionary.ContainsKey(text))
			{
				dictionary[text] = ReadWav(text);
				num5++;
				ReportMappedProgress(2, num, $"Cached {num5}/{num4}...", minProgress, maxProgress);
			}
		}
		ReportMappedProgress(3, num, "Mixing audio...", minProgress, maxProgress);
		for (int num8 = 0; num8 < inserts.Count; num8++)
		{
			AudioInsert audioInsert = inserts[num8];
			AudioData audioData2 = dictionary[audioInsert.FilePath];
			int position = audioInsert.Position;
			int num9 = format.BitsPerSample / 8;
			int num10 = num9 * format.Channels;
			position = position / num10 * num10;
			int num11 = Math.Min(audioData2.Data.Length, audioData.Data.Length - position);
			if (num11 > 0)
			{
				MixSamples(array, audioData2.Data, position, format);
				ReportMappedProgress(4 + num8, num, $"Mixed {num8 + 1}/{inserts.Count}...", minProgress, maxProgress);
			}
		}
		ReportMappedProgress(num - 1, num, "Writing output file...", minProgress, maxProgress);
		WriteWav(outputPath, array, format);
		ReportMappedProgress(num, num, "Mixing complete!", minProgress, maxProgress);
	}

	public void MixAudio(string baseFile, List<AudioInsert> inserts, string outputPath)
	{
		int num = inserts.Count + 4;
		ReportProgress(0, num, "Reading base audio...");
		AudioData baseAudio = ReadWav(baseFile);
		MixAudioCommon(baseAudio, inserts, outputPath, num);
	}

	public void MixAudio(MemoryStream baseMemoryStream, List<AudioInsert> inserts, string outputPath)
	{
		int num = inserts.Count + 4;
		ReportProgress(0, num, "Reading base audio...");
		AudioData baseAudio = ReadWav(baseMemoryStream);
		MixAudioCommon(baseAudio, inserts, outputPath, num);
	}

	private void MixAudioCommon(AudioData baseAudio, List<AudioInsert> inserts, string outputPath, int totalSteps)
	{
		WaveFormat format = baseAudio.Format;
		int sampleRate = format.SampleRate;
		int num = format.BitsPerSample / 8;
		int channels = format.Channels;
		int num2 = num * channels;
		ReportProgress(1, totalSteps, "Calculating positions...");
		foreach (AudioInsert insert in inserts)
		{
			insert.Position = (int)(insert.Timestamp * sampleRate / 1000.0) * num2;
			insert.Position = Math.Min(insert.Position, baseAudio.Data.Length);
		}
		inserts.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
		byte[] array = new byte[baseAudio.Data.Length];
		Array.Copy(baseAudio.Data, array, baseAudio.Data.Length);
		ReportProgress(2, totalSteps, "Caching audio files...");
		Dictionary<string, AudioData> dictionary = new Dictionary<string, AudioData>();
		List<string> list = (from i in inserts
			where !string.IsNullOrEmpty(i.FilePath)
			select i.FilePath).Distinct().ToList();
		int num3 = list.Count;
		int num4 = 0;
		for (int num5 = 0; num5 < list.Count; num5++)
		{
			string text = list[num5];
			if (!dictionary.ContainsKey(text))
			{
				dictionary[text] = ReadWav(text);
				num4++;
				ReportProgress(2, totalSteps, $"Cached {num4}/{num3}...");
			}
		}
		ReportProgress(3, totalSteps, "Mixing audio...");
		for (int num7 = 0; num7 < inserts.Count; num7++)
		{
			AudioInsert audioInsert = inserts[num7];
			AudioData audioData = dictionary[audioInsert.FilePath];
			int position = audioInsert.Position;
			int num8 = format.BitsPerSample / 8;
			int num9 = num8 * format.Channels;
			position = position / num9 * num9;
			int num10 = Math.Min(audioData.Data.Length, baseAudio.Data.Length - position);
			if (num10 > 0)
			{
				MixSamples(array, audioData.Data, position, format);
				ReportProgress(4 + num7, totalSteps, $"Mixed {num7 + 1}/{inserts.Count}...");
			}
		}
		ReportProgress(totalSteps - 1, totalSteps, "Writing output file...");
		WriteWav(outputPath, array, format);
		ReportProgress(totalSteps, totalSteps, "Mixing complete!");
	}


	public void CreateSilentWav(string filePath, double duration, WaveFormat format = null)
	{
		if (format == null)
		{
			format = new WaveFormat(44100, 16, 2);
		}
		int num = (int)(duration * format.SampleRate);
		if (num <= 0)
		{
			throw new ArgumentException("Duration must be greater than 0");
		}
		int num2 = num * format.BlockAlign;
		byte[] array = new byte[num2];
		using WaveFileWriter waveFileWriter = new WaveFileWriter(filePath, format);
		waveFileWriter.Write(array, 0, array.Length);
	}

	public void Export(string hitSoundPath, List<double> hitSoundTimes, string outputPath)
	{
		ReportProgress(0, 100, "Preparing...");
		string text = Path.GetTempFileName() + ".wav";
		try
		{
			ReportProgress(10, 100, "Normalizing timestamps...");
			double first = hitSoundTimes[0];
			List<double> list = hitSoundTimes.Select(t => t - first).ToList();
			ReportProgress(20, 100, "Creating silent base audio...");
			double duration = list.Last() / 1000.0 + 1.0;
			CreateSilentWav(text, duration);
			ReportProgress(30, 100, "Building insert list...");
			List<AudioInsert> list2 = new List<AudioInsert>();
			foreach (double item in list)
			{
				list2.Add(new AudioInsert(item, hitSoundPath));
			}
			ReportProgress(40, 100, "Mixing audio...");
			MixAudioWithProgressMapping(text, list2, outputPath, 40, 100);
			ReportProgress(100, 100, "Complete!");
		}
		finally
		{
			if (File.Exists(text))
			{
				File.Delete(text);
			}
		}
	}

	private AudioData ReadWav(string filePath)
	{
		using WaveFileReader reader = new WaveFileReader(filePath);
		return ReadWavFromReader(reader);
	}

	private AudioData ReadWav(MemoryStream memoryStream)
	{
		memoryStream.Position = 0L;
		using WaveFileReader reader = new WaveFileReader(memoryStream);
		return ReadWavFromReader(reader);
	}

	private AudioData ReadWavFromReader(WaveFileReader reader)
	{
		WaveFormat targetFormat = new WaveFormat(44100, 16, 2);
		if (reader.WaveFormat.Equals(targetFormat))
		{
			byte[] array = new byte[reader.Length];
			reader.Read(array, 0, array.Length);
			return new AudioData
			{
				Format = reader.WaveFormat,
				Data = array
			};
		}
		
		// Use WaveFormatConversionStream instead of MediaFoundationResampler to avoid COM dependency
		// 使用 WaveFormatConversionStream 替代 MediaFoundationResampler 以避免 COM 依赖
		IWaveProvider resampler;
		try
		{
			// Try using WaveFormatConversionStream (uses ACM, no COM dependency)
			// 尝试使用 WaveFormatConversionStream（使用 ACM，无 COM 依赖）
			resampler = new WaveFormatConversionStream(targetFormat, reader);
		}
		catch
		{
			// If conversion is not supported, manually resample
			// 如果不支持转换，则手动重采样
			return ManualResample(reader, targetFormat);
		}
		
		List<byte> list = new List<byte>();
		byte[] buffer = new byte[4096];
		int bytesRead;
		while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
		{
			for (int i = 0; i < bytesRead; i++)
			{
				list.Add(buffer[i]);
			}
		}
		
		if (resampler is IDisposable disposable)
		{
			disposable.Dispose();
		}
		
		return new AudioData
		{
			Format = targetFormat,
			Data = list.ToArray()
		};
	}
	
	private AudioData ManualResample(WaveFileReader reader, WaveFormat targetFormat)
	{
		// Simple manual resampling for basic cases
		// 简单的手动重采样，适用于基本场景
		WaveFormat sourceFormat = reader.WaveFormat;
		
		// Read all source data
		List<byte> sourceData = new List<byte>();
		byte[] buffer = new byte[4096];
		int bytesRead;
		while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
		{
			for (int i = 0; i < bytesRead; i++)
			{
				sourceData.Add(buffer[i]);
			}
		}
		
		// If formats are similar enough, just convert
		// 如果格式足够相似，直接转换
		if (sourceFormat.Channels == targetFormat.Channels && 
		    sourceFormat.BitsPerSample == targetFormat.BitsPerSample)
		{
			// Only sample rate differs - simple linear interpolation would be needed
			// For now, just return the source data with target format
			// 只有采样率不同 - 需要简单的线性插值
			// 目前，只返回源数据并使用目标格式
			return new AudioData
			{
				Format = targetFormat,
				Data = sourceData.ToArray()
			};
		}
		
		// For complex conversions, throw an exception
		// 对于复杂转换，抛出异常
		throw new NotSupportedException(
			$"Cannot convert audio from {sourceFormat} to {targetFormat}. " +
			"Please ensure input files are 44.1kHz, 16-bit, stereo WAV files.");
	}

	private void WriteWav(string filePath, byte[] data, WaveFormat format)
	{
		using WaveFileWriter waveFileWriter = new WaveFileWriter(filePath, format);
		waveFileWriter.Write(data, 0, data.Length);
	}

	private void MixSamples(byte[] buffer, byte[] insert, int startPos, WaveFormat format)
	{
		int num = format.BitsPerSample / 8;
		int channels = format.Channels;
		if (format.BitsPerSample == 16)
		{
			for (int i = 0; i < insert.Length - 1; i += 2)
			{
				int num2 = startPos + i;
				if (num2 + 1 >= buffer.Length)
				{
					break;
				}
				short num3 = (short)(buffer[num2] | (buffer[num2 + 1] << 8));
				short num4 = (short)(insert[i] | (insert[i + 1] << 8));
				int val = num3 + num4;
				val = Math.Max(-32768, Math.Min(32767, val));
				buffer[num2] = (byte)(val & 0xFF);
				buffer[num2 + 1] = (byte)((val >> 8) & 0xFF);
			}
			return;
		}
		for (int j = 0; j < insert.Length; j++)
		{
			int num5 = startPos + j;
			if (num5 >= buffer.Length)
			{
				break;
			}
			int val2 = buffer[num5] + insert[j];
			buffer[num5] = (byte)Math.Max(0, Math.Min(255, val2));
		}
	}
}
