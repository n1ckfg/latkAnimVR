using AnimVRFilePlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using SimpleJSON;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

[CustomImporter(Extension = "latk")]
public class latkAnimVR : CustomImporter
{

    public override List<PlayableData> Import(string path) {
        SymbolData result = new SymbolData();
        result.displayName = Path.GetFileNameWithoutExtension(path);

        readLatkStrokes(ref result, path);

        return new List<PlayableData>() { result };
    }

    public override void Export(StageData stage, string path) {
        //File.WriteAllText(path, stage.name);
        writeLatkStrokes(ref stage, path, stage.name);
    }

    // ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~

    public void writeLatkStrokes(ref StageData stage, string path, string writeFileName) {
        List<string> FINAL_LAYER_LIST = new List<string>();

        for (int gg = 0; gg < stage.Symbols.Count; gg++) {

            for (int hh = 0; hh < stage.Symbols[gg].TimeLines.Count; hh++) {
                int currentLayer = hh;

                List<string> sb = new List<string>();
                List<string> sbHeader = new List<string>();
                sbHeader.Add("\t\t\t\t\t\"frames\":[");
                sb.Add(string.Join("\n", sbHeader.ToArray()));

                for (int h = 0; h < stage.Symbols[gg].TimeLines[currentLayer].Frames.Count; h++) {
                    int currentFrame = h;

                    List<string> sbbHeader = new List<string>();
                    sbbHeader.Add("\t\t\t\t\t\t{");
                    sbbHeader.Add("\t\t\t\t\t\t\t\"strokes\":[");
                    sb.Add(string.Join("\n", sbbHeader.ToArray()));
                    for (int i = 0; i < stage.Symbols[gg].TimeLines[currentLayer].Frames[currentFrame].Lines.Count; i++) {
                        List<string> sbb = new List<string>();
                        sbb.Add("\t\t\t\t\t\t\t\t{");
                        float r = stage.Symbols[gg].TimeLines[currentLayer].Frames[currentFrame].Lines[i].colors[0].r;
                        float g = stage.Symbols[gg].TimeLines[currentLayer].Frames[currentFrame].Lines[i].colors[0].g;
                        float b = stage.Symbols[gg].TimeLines[currentLayer].Frames[currentFrame].Lines[i].colors[0].b;
                        sbb.Add("\t\t\t\t\t\t\t\t\t\"color\":[" + r + ", " + g + ", " + b + "],");

                        if (stage.Symbols[gg].TimeLines[currentLayer].Frames[currentFrame].Lines[i].Points.Count > 0) {
                            sbb.Add("\t\t\t\t\t\t\t\t\t\"points\":[");
                            for (int j = 0; j < stage.Symbols[gg].TimeLines[currentLayer].Frames[currentFrame].Lines[i].Points.Count; j++) {
                                float x = stage.Symbols[gg].TimeLines[currentLayer].Frames[currentFrame].Lines[i].Points[j].x;
                                float y = stage.Symbols[gg].TimeLines[currentLayer].Frames[currentFrame].Lines[i].Points[j].y;
                                float z = stage.Symbols[gg].TimeLines[currentLayer].Frames[currentFrame].Lines[i].Points[j].z;

                                if (j == stage.Symbols[gg].TimeLines[currentLayer].Frames[currentFrame].Lines[i].Points.Count - 1) {
                                    sbb.Add("\t\t\t\t\t\t\t\t\t\t{\"co\":[" + x + ", " + y + ", " + z + "], \"pressure\":1, \"strength\":1}");
                                    sbb.Add("\t\t\t\t\t\t\t\t\t]");
                                } else {
                                    sbb.Add("\t\t\t\t\t\t\t\t\t\t{\"co\":[" + x + ", " + y + ", " + z + "], \"pressure\":1, \"strength\":1},");
                                }
                            }
                        } else {
                            sbb.Add("\t\t\t\t\t\t\t\t\t\"points\":[]");
                        }

                        if (i == stage.Symbols[gg].TimeLines[currentLayer].Frames[currentFrame].Lines.Count - 1) {
                            sbb.Add("\t\t\t\t\t\t\t\t}");
                        } else {
                            sbb.Add("\t\t\t\t\t\t\t\t},");
                        }

                        sb.Add(string.Join("\n", sbb.ToArray()));
                    }

                    List<string> sbFooter = new List<string>();
                    if (h == stage.Symbols[gg].TimeLines[currentLayer].Frames.Count - 1) {
                        sbFooter.Add("\t\t\t\t\t\t\t]");
                        sbFooter.Add("\t\t\t\t\t\t}");
                    } else {
                        sbFooter.Add("\t\t\t\t\t\t\t]");
                        sbFooter.Add("\t\t\t\t\t\t},");
                    }
                    sb.Add(string.Join("\n", sbFooter.ToArray()));
                }

                FINAL_LAYER_LIST.Add(string.Join("\n", sb.ToArray()));
            }
        }

        List<string> s = new List<string>();
        s.Add("{");
        s.Add("\t\"creator\": \"unity\",");
        s.Add("\t\"grease_pencil\":[");
        s.Add("\t\t{");
        s.Add("\t\t\t\"layers\":[");

        for (int gg = 0; gg < stage.Symbols.Count; gg++) {

            for (int i = 0; i < stage.Symbols[gg].TimeLines.Count; i++) {
                int currentLayer = i;

                s.Add("\t\t\t\t{");
                if (stage.Symbols[gg].TimeLines[currentLayer].name != null && stage.Symbols[gg].TimeLines[currentLayer].name != "") {
                    s.Add("\t\t\t\t\t\"name\": \"" + stage.Symbols[gg].TimeLines[currentLayer].name + "\",");
                } else {
                    s.Add("\t\t\t\t\t\"name\": \"UnityLayer " + (currentLayer + 1) + "\",");
                }

                s.Add(FINAL_LAYER_LIST[currentLayer]);

                s.Add("\t\t\t\t\t]");
                if (currentLayer < stage.Symbols[gg].TimeLines.Count - 1) {
                    s.Add("\t\t\t\t},");
                } else {
                    s.Add("\t\t\t\t}");
                }
            }
        }
        
        s.Add("            ]"); // end layers
        s.Add("        }");
        s.Add("    ]");
        s.Add("}");

        string extO = ".latk";
        string tempName = writeFileName.Replace(extO, "");
        int timestamp = (int)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
        tempName += "_" + timestamp + extO;

        string url = Path.Combine(path, tempName);

        saveJsonAsZip(url, tempName, string.Join("\n", s.ToArray()));
    }

    public void readLatkStrokes(ref SymbolData symbol, string path) {
        JSONNode jsonNode = getJsonFromZip(File.ReadAllBytes(path));

        for (int f = 0; f < jsonNode["grease_pencil"][0]["layers"].Count; f++) {
            int currentLayer = f;
            TimeLineData layer = new TimeLineData();
            symbol.TimeLines.Add(layer);
            symbol.TimeLines[currentLayer].name = jsonNode["grease_pencil"][0]["layers"][f]["name"];

            for (int h = 0; h < jsonNode["grease_pencil"][0]["layers"][f]["frames"].Count; h++) {
                int currentFrame = h;
                FrameData frame = new FrameData();
                symbol.TimeLines[currentLayer].Frames.Add(frame);

                try {
                    float px = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["parent_location"][0].AsFloat / 10f;
                    float py = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["parent_location"][2].AsFloat / 10f;
                    float pz = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["parent_location"][1].AsFloat / 10f;
                    //symbol.TimeLines[currentLayer].Frames[currentFrame].transform.pos = new Vector3(px, py, pz);
                } catch (UnityException e) { }

                for (int i = 0; i < jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"].Count; i++) {
                    float r = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"][i]["color"][0].AsFloat;
                    float g = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"][i]["color"][1].AsFloat;
                    float b = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"][i]["color"][2].AsFloat;
                    Color c = new Color(r, g, b);

                    LineData stroke = new LineData();
                    symbol.TimeLines[currentLayer].Frames[currentFrame].Lines.Add(stroke);

                    for (int j = 0; j < jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"][i]["points"].Count; j++) {
                        float x = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"][i]["points"][j]["co"][0].AsFloat;
                        float y = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"][i]["points"][j]["co"][1].AsFloat;
                        float z = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"][i]["points"][j]["co"][2].AsFloat;

                        Vector3 p = new Vector3(x, y, z); //applyTransformMatrix(new Vector3(x, y, z));

                        symbol.TimeLines[currentLayer].Frames[currentFrame].Lines[symbol.TimeLines[currentLayer].Frames[currentFrame].Lines.Count - 1].Points.Add(p);
                        symbol.TimeLines[currentLayer].Frames[currentFrame].Lines[symbol.TimeLines[currentLayer].Frames[currentFrame].Lines.Count - 1].colors.Add(c);
                    }
                }
            }
        }
    }

    /*
    private Matrix4x4 transformMatrix;

    private void updateTransformMatrix() {
        transformMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
    }

    private Vector3 applyTransformMatrix(Vector3 p) {
        return transformMatrix.MultiplyPoint3x4(p);
    }
    */

    private JSONNode getJsonFromZip(byte[] bytes) {
        MemoryStream fileStream = new MemoryStream(bytes, 0, bytes.Length);
        ZipFile zipFile = new ZipFile(fileStream);

        foreach (ZipEntry entry in zipFile) {
            if (Path.GetExtension(entry.Name).ToLower() == ".json") {
                Stream zippedStream = zipFile.GetInputStream(entry);
                StreamReader read = new StreamReader(zippedStream, true);
                string json = read.ReadToEnd();
                Debug.Log(json);
                return JSON.Parse(json);
            }
        }

        return null;
    }

    private void saveJsonAsZip(string url, string fileName, string s) {
        MemoryStream memStreamIn = new MemoryStream();
        StreamWriter writer = new StreamWriter(memStreamIn);
        writer.Write(s);
        writer.Flush();
        memStreamIn.Position = 0;

        MemoryStream outputMemStream = new MemoryStream();
        ZipOutputStream zipStream = new ZipOutputStream(outputMemStream);

        zipStream.SetLevel(3); // 0-9

        string fileNameMinusExtension = "";
        string[] nameTemp = fileName.Split('.');
        for (int i = 0; i < nameTemp.Length - 1; i++) {
            fileNameMinusExtension += nameTemp[i];
        }

        ZipEntry newEntry = new ZipEntry(fileNameMinusExtension + ".json");
        newEntry.DateTime = System.DateTime.Now;

        zipStream.PutNextEntry(newEntry);

        StreamUtils.Copy(memStreamIn, zipStream, new byte[4096]);
        zipStream.CloseEntry();

        zipStream.IsStreamOwner = false;
        zipStream.Close();

        outputMemStream.Position = 0;

        using (FileStream file = new FileStream(url, FileMode.Create, System.IO.FileAccess.Write)) {
            byte[] bytes = new byte[outputMemStream.Length];
            outputMemStream.Read(bytes, 0, (int)outputMemStream.Length);
            file.Write(bytes, 0, bytes.Length);
            outputMemStream.Close();
        }
    }

}
