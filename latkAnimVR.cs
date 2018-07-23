using AnimVRFilePlugin;
using System;
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
        var lines = File.ReadAllLines(path);

        SymbolData result = new SymbolData();
        result.displayName = Path.GetFileNameWithoutExtension(path);

        string basePath = Path.GetDirectoryName(path) + "/";

        int currentFrame = 0;
        foreach (var line in lines) {
            var parts = line.Split(' ');
            float duration = float.Parse(parts[0]);
            string modelFile = basePath + parts[1];

            List<StaticMeshData> meshes;
            if (!AnimAssimpImporter.ImportFile(modelFile, false, out meshes)) continue;

            int frameDuration = Mathf.FloorToInt(duration * 12);

            foreach (StaticMeshData meshData in meshes) {
                meshData.AbsoluteTimeOffset = currentFrame;
                meshData.Timeline.Frames.Clear();
                meshData.InstanceMap.Clear();
                meshData.LoopIn = AnimVR.LoopType.OneShot;
                meshData.LoopOut = AnimVR.LoopType.OneShot;

                for (int i = 0; i < frameDuration; i++) {
                    var frame = new SerializableTransform();
                    meshData.Timeline.Frames.Add(frame);
                    meshData.InstanceMap.Add(frame);
                }

                result.Playables.Add(meshData);
            }

            currentFrame += frameDuration;
        }

        return new List<PlayableData>() { result };
    }

    public override void Export(StageData stage, string path) {
        //File.WriteAllText(path, stage.name);
        writeLatkStrokes(ref stage, path, stage.name);
    }

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

    /*
    public IEnumerator readLatkStrokes() {
        Debug.Log("*** Begin reading...");
        isReadingFile = true;

        string ext = Path.GetExtension(readFileName).ToLower();
        Debug.Log("Found extension " + ext);
        bool useZip = (ext == ".latk" || ext == ".zip");

        for (int h = 0; h < layerList.Count; h++) {
            for (int i = 0; i < layerList[h].Frames.Count; i++) {
                Destroy(layerList[h].Frames[i].gameObject);
            }
            Destroy(layerList[h].gameObject);
        }
        layerList = new List<LatkLayer>();

        string url = "";

#if UNITY_ANDROID
		url = Path.Combine("jar:file://" + Application.dataPath + "!/assets/", readFileName);
#endif

#if UNITY_IOS
		url = Path.Combine("file://" + Application.dataPath + "/Raw", readFileName);
#endif

#if UNITY_EDITOR
        url = Path.Combine("file://" + Application.dataPath, readFileName);
#endif

#if UNITY_STANDALONE_WIN
        url = Path.Combine("file://" + Application.dataPath, readFileName);
#endif

#if UNITY_STANDALONE_OSX
		url = Path.Combine("file://" + Application.dataPath, readFileName);		
#endif

#if UNITY_WSA
		url = Path.Combine("file://" + Application.dataPath, readFileName);		
#endif

        WWW www = new WWW(url);
        yield return www;

        Debug.Log("+++ File reading finished. Begin parsing...");
        yield return new WaitForSeconds(consoleUpdateInterval);

        if (useZip) {
            jsonNode = getJsonFromZip(www.bytes);
        } else {
            jsonNode = JSON.Parse(www.text);
        }

        for (int f = 0; f < jsonNode["grease_pencil"][0]["layers"].Count; f++) {
            instantiateLayer();
            currentLayer = f;
            layerList[currentLayer].name = jsonNode["grease_pencil"][0]["layers"][f]["name"];
            for (int h = 0; h < jsonNode["grease_pencil"][0]["layers"][f]["frames"].Count; h++) {
                Debug.Log("Starting frame " + (layerList[currentLayer].currentFrame + 1) + ".");
                instantiateFrame();
                layerList[currentLayer].currentFrame = h;

                try {
                    float px = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["parent_location"][0].AsFloat / 10f;
                    float py = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["parent_location"][2].AsFloat / 10f;
                    float pz = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["parent_location"][1].AsFloat / 10f;
                    layerList[currentLayer].Frames[h].parentPos = new Vector3(px, py, pz);
                } catch (UnityException e) { }

                for (int i = 0; i < jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"].Count; i++) {
                    float r = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"][i]["color"][0].AsFloat;
                    float g = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"][i]["color"][1].AsFloat;
                    float b = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"][i]["color"][2].AsFloat;
                    Color c = new Color(r, g, b);

                    instantiateStroke(c);
                    for (int j = 0; j < jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"][i]["points"].Count; j++) {
                        float x = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"][i]["points"][j]["co"][0].AsFloat;
                        float y = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"][i]["points"][j]["co"][1].AsFloat;
                        float z = jsonNode["grease_pencil"][0]["layers"][f]["frames"][h]["strokes"][i]["points"][j]["co"][2].AsFloat;

                        Vector3 p = applyTransformMatrix(new Vector3(x, y, z));

                        layerList[currentLayer].Frames[layerList[currentLayer].currentFrame].Lines[layerList[currentLayer].Frames[layerList[currentLayer].currentFrame].Lines.Count - 1].points.Add(p);
                    }

                    Debug.Log("Adding frame " + (layerList[currentLayer].currentFrame + 1) + ": stroke " + (i + 1) + " of " + layerList[currentLayer].Frames[layerList[currentLayer].currentFrame].Lines.Count + ".");
                }
                if (textMesh != null) textMesh.text = "READING " + (layerList[currentLayer].currentFrame + 1) + " / " + jsonNode["grease_pencil"][0]["layers"][0]["frames"].Count;
                Debug.Log("Ending frame " + (layerList[currentLayer].currentFrame + 1) + ".");
                yield return new WaitForSeconds(consoleUpdateInterval);
            }


            for (int h = 0; h < layerList[currentLayer].Frames.Count; h++) {
                layerList[currentLayer].currentFrame = h;
                layerList[currentLayer].Frames[layerList[currentLayer].currentFrame].isDirty = true;
                if (checkEmptyFrame(layerList[currentLayer].currentFrame)) {
                    if (fillEmptyMethod == FillEmptyMethod.WRITE) {
                        copyFramePointsForward(layerList[currentLayer].currentFrame);
                    }
                }
            }

            layerList[currentLayer].currentFrame = 0;

            for (int h = 0; h < layerList[currentLayer].Frames.Count; h++) {
                if (h != layerList[currentLayer].currentFrame) {
                    layerList[currentLayer].Frames[h].showFrame(false);
                } else {
                    layerList[currentLayer].Frames[h].showFrame(true);
                }
            }
        }

        if (newLayerOnRead) {
            instantiateLayer();
            currentLayer = layerList.Count - 1;
            instantiateFrame();
        }

        Debug.Log("*** Read " + url);
        isReadingFile = false;
        if (playOnStart) isPlaying = true;
    }
    */

    void saveJsonAsZip(string url, string fileName, string s) {
        // https://stackoverflow.com/questions/1879395/how-do-i-generate-a-stream-from-a-string
        // https://github.com/icsharpcode/SharpZipLib/wiki/Zip-Samples
        // https://stackoverflow.com/questions/8624071/save-and-load-memorystream-to-from-a-file

        MemoryStream memStreamIn = new MemoryStream();
        StreamWriter writer = new StreamWriter(memStreamIn);
        writer.Write(s);
        writer.Flush();
        memStreamIn.Position = 0;

        MemoryStream outputMemStream = new MemoryStream();
        ZipOutputStream zipStream = new ZipOutputStream(outputMemStream);

        zipStream.SetLevel(3); //0-9, 9 being the highest level of compression

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

        zipStream.IsStreamOwner = false;    // False stops the Close also Closing the underlying stream.
        zipStream.Close();          // Must finish the ZipOutputStream before using outputMemStream.

        outputMemStream.Position = 0;

        using (FileStream file = new FileStream(url, FileMode.Create, System.IO.FileAccess.Write)) {
            byte[] bytes = new byte[outputMemStream.Length];
            outputMemStream.Read(bytes, 0, (int)outputMemStream.Length);
            file.Write(bytes, 0, bytes.Length);
            outputMemStream.Close();
        }

        /*
        // Alternative outputs:
        // ToArray is the cleaner and easiest to use correctly with the penalty of duplicating allocated memory.
        byte[] byteArrayOut = outputMemStream.ToArray();

        // GetBuffer returns a raw buffer raw and so you need to account for the true length yourself.
        byte[] byteArrayOut = outputMemStream.GetBuffer();
        long len = outputMemStream.Length;
        */
    }

}
