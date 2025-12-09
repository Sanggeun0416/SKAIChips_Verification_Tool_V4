using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    #region DTOs

    public class AutoTaskProjectFile
    {
        public string ProjectName { get; set; }
        public List<AutoTaskFileItem> Tasks { get; set; } = new List<AutoTaskFileItem>();
    }

    public class AutoTaskFileItem
    {
        public string Name { get; set; }
        public List<AutoTaskStepFile> Steps { get; set; } = new List<AutoTaskStepFile>();
    }

    public class AutoTaskStepFile
    {
        public string Type { get; set; }
        public string Title { get; set; }

        public string Address { get; set; }
        public string Value { get; set; }
        public int DelayMs { get; set; }
    }

    #endregion

    public static class AutoTaskStorage
    {
        #region Path Helpers

        private static string GetFolderPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dir = Path.Combine(baseDir, "AutoTasks");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return dir;
        }

        private static string GetFilePath(string projectName)
        {
            var safeName = string.IsNullOrWhiteSpace(projectName)
                ? "UnknownProject"
                : projectName.Replace(" ", "_");

            return Path.Combine(GetFolderPath(), safeName + "_AutoTasks.json");
        }

        #endregion

        #region Value Helpers

        private static string ToHex8(uint v) => "0x" + v.ToString("X8");

        private static uint ParseHex(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            text = text.Trim();

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            uint v;
            if (uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v))
                return v;

            return 0;
        }

        #endregion

        #region Save

        public static void Save(string projectName, IEnumerable<ScriptAutoTask> tasks)
        {
            var file = new AutoTaskProjectFile
            {
                ProjectName = projectName
            };

            if (tasks != null)
            {
                foreach (var t in tasks)
                {
                    if (t == null)
                        continue;

                    var item = new AutoTaskFileItem
                    {
                        Name = t.Name
                    };

                    var def = t.Definition;
                    if (def != null && def.Blocks != null)
                    {
                        foreach (var block in def.Blocks)
                        {
                            if (block == null)
                                continue;

                            var step = new AutoTaskStepFile
                            {
                                Title = block.Title ?? string.Empty
                            };

                            if (block is DelayBlock d)
                            {
                                step.Type = "Delay";
                                step.DelayMs = d.Milliseconds;
                            }
                            else if (block is RegWriteBlock w)
                            {
                                step.Type = "RegWrite";
                                step.Address = ToHex8(w.Address);
                                step.Value = ToHex8(w.Value);
                            }
                            else if (block is RegReadBlock r)
                            {
                                step.Type = "RegRead";
                                step.Address = ToHex8(r.Address);
                                step.Value = string.IsNullOrWhiteSpace(r.ResultKey)
                                    ? "LastReadValue"
                                    : r.ResultKey;
                            }
                            else
                            {
                                continue;
                            }

                            item.Steps.Add(step);
                        }
                    }

                    file.Tasks.Add(item);
                }
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(file, options);
            File.WriteAllText(GetFilePath(projectName), json, Encoding.UTF8);
        }

        #endregion

        #region Load

        public static List<ScriptAutoTask> Load(string projectName)
        {
            var result = new List<ScriptAutoTask>();
            var path = GetFilePath(projectName);

            if (!File.Exists(path))
                return result;

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                var file = JsonSerializer.Deserialize<AutoTaskProjectFile>(json);

                if (file == null || file.Tasks == null)
                    return result;

                foreach (var t in file.Tasks)
                {
                    if (t == null || string.IsNullOrWhiteSpace(t.Name))
                        continue;

                    var task = new ScriptAutoTask(t.Name);
                    var def = task.Definition;

                    def.Blocks.Clear();

                    if (t.Steps == null)
                    {
                        result.Add(task);
                        continue;
                    }

                    foreach (var s in t.Steps)
                    {
                        if (s == null || string.IsNullOrWhiteSpace(s.Type))
                            continue;

                        AutoBlockBase block = null;

                        switch (s.Type)
                        {
                            case "Delay":
                                block = new DelayBlock
                                {
                                    Title = string.IsNullOrWhiteSpace(s.Title) ? "Delay" : s.Title,
                                    Milliseconds = s.DelayMs
                                };
                                break;

                            case "RegWrite":
                                var addrW = ParseHex(s.Address);
                                var valueW = ParseHex(s.Value);
                                var bw = new RegWriteBlock(addrW, valueW);
                                bw.Title = string.IsNullOrWhiteSpace(s.Title) ? "RegWrite" : s.Title;
                                block = bw;
                                break;

                            case "RegRead":
                                var addrR = ParseHex(s.Address);
                                var br = new RegReadBlock(addrR);
                                br.Title = string.IsNullOrWhiteSpace(s.Title) ? "RegRead" : s.Title;
                                br.ResultKey = string.IsNullOrWhiteSpace(s.Value) ? "LastReadValue" : s.Value;
                                block = br;
                                break;
                        }

                        if (block != null)
                            def.Blocks.Add(block);
                    }

                    result.Add(task);
                }
            }
            catch
            {
            }

            return result;
        }

        #endregion
    }
}
