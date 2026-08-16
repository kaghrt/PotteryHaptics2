using System.IO;
using UnityEditor;
using UnityEngine;

namespace PotteryHaptics.Core.Editor
{
    /// <summary>
    /// Assets/_Project/ScriptableObjects/Calibration/HapticCalibrationConfig.asset を
    /// 唯一の情報源として取得する。存在しなければ仕様書記載の暫定値で新規作成する。
    /// </summary>
    public static class CalibrationConfigProvider
    {
        public const string AssetPath = "Assets/_Project/ScriptableObjects/Calibration/HapticCalibrationConfig.asset";

        public static HapticCalibrationConfig GetOrCreate()
        {
            HapticCalibrationConfig config = AssetDatabase.LoadAssetAtPath<HapticCalibrationConfig>(AssetPath);
            if (config != null)
            {
                return config;
            }

            Directory.CreateDirectory("Assets/_Project/ScriptableObjects/Calibration");
            config = ScriptableObject.CreateInstance<HapticCalibrationConfig>();
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
            return config;
        }
    }
}
