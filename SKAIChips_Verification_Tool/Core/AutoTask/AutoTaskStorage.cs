using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using SKAIChips_Verification_Tool.Core.AutoTask;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public class AutoTaskProjectFile
    {
        public string ProjectName { get; set; }
        public List<AutoTaskFileItem> Tasks { get; set; } = new();
    }

    public class AutoTaskFileItem
    {
        public string Name { get; set; }
        public List<AutoTaskStepFile> Steps { get; set; } = new();
    }

    public class AutoTaskStepFile
    {
        public string Type { get; set; }
        public string Title { get; set; }

        public string Address { get; set; }
        public string Value { get; set; }
        public int DelayMs { get; set; }
    }

    public static class AutoTaskStorage
    {
        private static string GetFolderPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dir = Path.Combine(baseDir, "AutoTasks");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        private static string GetFilePath(string projectName)
        {
            string safeName = string.IsNullOrWhiteSpace(projectName)
                ? "UnknownProject"
                : projectName.Replace(" ", "_");

            return Path.Combine(GetFolderPath(), $"{safeName}_AutoTasks.json");
        }

        private static string ToHex8(uint v)
        {
            return $"0x{v:X8}";
        }

        private static uint ParseHex(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            text = text.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            if (uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
                return v;

            return 0;
        }

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
                    if (t == null) continue;

                    var item = new AutoTaskFileItem
                    {
                        Name = t.Name
                    };

                    var def = t.Definition;
                    if (def?.Blocks != null)
                    {
                        foreach (var block in def.Blocks)
                        {
                            if (block == null) continue;

                            var step = new AutoTaskStepFile
                            {
                                Title = block.Title ?? ""
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

            string json = JsonSerializer.Serialize(file, options);
            File.WriteAllText(GetFilePath(projectName), json, Encoding.UTF8);
        }

        public static List<ScriptAutoTask> Load(string projectName)
        {
            var result = new List<ScriptAutoTask>();
            string path = GetFilePath(projectName);

            if (!File.Exists(path))
                return result;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var file = JsonSerializer.Deserialize<AutoTaskProjectFile>(json);

                if (file?.Tasks == null)
                    return result;

                foreach (var t in file.Tasks)
                {
                    if (string.IsNullOrWhiteSpace(t.Name))
                        continue;

                    var task = new ScriptAutoTask(t.Name);
                    var def = task.Definition;

                    def.Blocks.Clear();

                    if (t.Steps != null)
                    {
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
                                    {
                                        uint addr = ParseHex(s.Address);
                                        uint value = ParseHex(s.Value);
                                        var b = new RegWriteBlock(addr, value);
                                        b.Title = string.IsNullOrWhiteSpace(s.Title) ? "RegWrite" : s.Title;
                                        block = b;
                                        break;
                                    }

                                case "RegRead":
                                    {
                                        uint addr = ParseHex(s.Address);
                                        var b = new RegReadBlock(addr);
                                        b.Title = string.IsNullOrWhiteSpace(s.Title) ? "RegRead" : s.Title;
                                        b.ResultKey = string.IsNullOrWhiteSpace(s.Value) ? "LastReadValue" : s.Value;
                                        block = b;
                                        break;
                                    }
                            }

                            if (block != null)
                                def.Blocks.Add(block);
                        }
                    }

                    result.Add(task);
                }
            }
            catch
            {
            }

            return result;
        }
    }
}
