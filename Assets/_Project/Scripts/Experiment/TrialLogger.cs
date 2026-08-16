using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace PotteryHaptics.Experiment
{
    /// <summary>
    /// TrialRecordをCSVとして Application.persistentDataPath/Logs/{participantId}_{phase}_{timestamp}.csv
    /// に出力する。両実験で共通のフォーマットを使うため、分析コードを使い回せる。
    /// </summary>
    public class TrialLogger
    {
        private const string HeaderLine =
            "participantId,phase,visualCondition,trialIndex,textureType,stimulusName,physicalValue," +
            "relativeIntensityPercent,intensityLevel,responseValue,correct,reactionTimeSec,timestampIso";

        private readonly string filePath;
        private StreamWriter writer;

        public string FilePath => filePath;

        public TrialLogger(string participantId, string phase)
        {
            string logDir = Path.Combine(Application.persistentDataPath, "Logs");
            Directory.CreateDirectory(logDir);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            filePath = Path.Combine(logDir, $"{participantId}_{phase}_{timestamp}.csv");
        }

        public void Open()
        {
            writer = new StreamWriter(filePath, false, Encoding.UTF8);
            writer.WriteLine(HeaderLine);
            writer.Flush();
        }

        public void WriteRecord(TrialRecord record)
        {
            if (writer == null)
            {
                Open();
            }

            string line = string.Join(",",
                Escape(record.participantId),
                Escape(record.phase),
                record.visualCondition.ToString(),
                record.trialIndex.ToString(),
                record.textureType.ToString(),
                Escape(record.stimulusName),
                record.physicalValue.ToString("F4"),
                record.relativeIntensityPercent.ToString("F2"),
                record.intensityLevel.ToString(),
                Escape(record.responseValue),
                record.correct.ToString(),
                record.reactionTimeSec.ToString("F4"),
                Escape(record.timestampIso));

            writer.WriteLine(line);
            writer.Flush();
        }

        public void Close()
        {
            writer?.Close();
            writer = null;
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}
