/*
参考サイト
三角形と線分の交差判定 | 試行錯誤
https://shikousakugo.wordpress.com/2012/05/29/ray-intersection/

[Unity] アウトライン Outline - 詳細篇　モデル拡大法 - Qiita
https://qiita.com/kerrot/items/51b7ab5b3151c066fcbc

Flat shading - Unity Answers
https://answers.unity.com/questions/798510/flat-shading.html

【Unity】エディタ右下に表示されるプログレスバーをスクリプトから操作する方法 - コガネブログ
http://baba-s.hatenablog.com/entry/2017/11/13/144824

*/

using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;
using System.Linq;

using Object = UnityEngine.Object;

namespace TransferAttributes
{
    using TAP = TransferAttributes.TransferAttributesProcess;
    public class TransferAttributesWindow : EditorWindow
    {
        private static TransferAttributesWindow window;
        private static readonly Vector2         defaultWindowSize = new Vector2(400f, 440f);
        private static Vector2                  windowSize;

        private Object              srcObj;
        private Mesh                srcMesh;
        private GameObject          srcGameObj;
        private MeshFilter          srcMF;
        private SkinnedMeshRenderer srcSMR;
        private Transform           srcTrans;

        private static float  progress         = 0f;
        private static string progressInfo     = "";
        private static bool   isProgressCancel = false;


        private float voxelSpaceSize = Mathf.Infinity;
        private Dictionary<Vector3, Bounds> voxelSpaceDict;

        class DstMeshData
        {
            public Object              dstObj;
            public Mesh                dstMesh;
            public GameObject          dstGameObj;
            public MeshFilter          dstMF;
            public SkinnedMeshRenderer dstSMR;
        }
        private List<DstMeshData> dstMeshDataList = new List<DstMeshData>(){ new DstMeshData() };


        private static bool  toggleMeshOverwrite           = true;
        private const string TOGGLE_MESH_OVERWRITE_KEY     = "TransferAttributesWindow toggleMeshOverwrite";

        private static bool  toggleVtxColorTransfer        = true;
        private const string TOGGLE_VTX_COLOR_TRANSFER_KEY = "TransferAttributesWindow toggleVtxColorsTransfer";

        private static bool  toggleVtxNmlTransfer          = true;
        private const string TOGGLE_VTX_NML_TRANSFER_KEY   = "TransferAttributesWindow toggleVtxNmlTransfer";

        private static bool  toggleMoveEachVtx             = true;
        private const string TOGGLE_MOVE_EACH_VTX_KEY      = "TransferAttributesWindow toggleMoveEachVtx";

        private static bool  toggleBoneWtTransfer          = true;
        private const string TOGGLE_BONE_WT_TRANSFER_KEY   = "TransferAttributesWindow toggleBoneWtsTransfer";

        private static bool  toggleStoreAveragedNml        = true;
        private const string TOGGLE_STORE_AVERAGED_NML_KEY = "TransferAttributesWindow toggleStoreAveragedNml";

        private static bool  toggleStoreVtxColor           = true;
        private const string TOGGLE_STORE_VTX_COLOR_KEY    = "TransferAttributesWindow toggleStoreVtxColor";

        private static bool  toggleMeshCompress            = true;
        private const string TOGGLE_MESH_COMPRESS_KEY      = "TransferAttributesWindow toggleMeshCompress";

        private static bool  toggleReadableMesh            = true;
        private const string TOGGLE_READABLE_MESH_KEY      = "TransferAttributesWindow toggleReadableMesh";


        private enum TypeOfSearchMethod
        {
            UseAveragedNormal,
            UseVertexPosition
        }
        private static TypeOfSearchMethod typeOfSearchMethod = TypeOfSearchMethod.UseAveragedNormal;
        private const string TYPE_OF_SEARCH_METHOD = "TransferAttributesWindow typeOfSearchMethod";
        private static string[] typeOfSearchMethodDispNames = new string[2];


        private enum TypeOfStoreNml
        {
            Tangent,
            UV_Channel_1,
            UV_Channel_2,
            UV_Channel_3,
            UV_Channel_4,
            Normal
        }
        // 初期値は Tangent
        private static TypeOfStoreNml typeOfStoreNml = TypeOfStoreNml.Tangent; 
        private const string TYPE_OF_STORE_NML_KEY = "TransferAttributesWindow typeOfStoreNml";

        private enum TypeOfStoreVtxColor
        {
            Vertex_Color,
            UV_Channel_1,
            UV_Channel_2,
            UV_Channel_3,
            UV_Channel_4
        }
        // 初期値は Vertex_Color
        private static TypeOfStoreVtxColor typeOfStoreVtxColor = TypeOfStoreVtxColor.Vertex_Color; 
        private const string TYPE_OF_STORE_VTX_COLOR_KEY = "TransferAttributesWindow typeOfStoreVtxColor";
        private static Texture2D texOfStoreVtxColor;
        private static TextureImporter texOfStoreVtxColorImporter;
        private static bool texOfStoreVtxColorReadable = false;

        // 初期値は Off
        private static ModelImporterMeshCompression meshCompressOption = ModelImporterMeshCompression.Off;
        private const string MESH_COMPRESS_OPTION_KEY = "TransferAttributesWindow meshCompressOption";
        
        // 初期値は Vertex_Color
        private static bool isReadableMesh = false;
        private const string IS_READABLE_MESH_KEY = "TransferAttributesWindow isReadableMesh";


        public enum TypeOfUseUV
        {
            UV_Channel_1 = 0,
            UV_Channel_2 = 1,
            UV_Channel_3 = 2,
            UV_Channel_4 = 3
        }
        // 初期値は UV_Channel_1
        public static TypeOfUseUV typeOfUseUV = TypeOfUseUV.UV_Channel_1; 


        private static class Messages
        {
            public static GUIContent label_SrcMesh             = new GUIContent();
            public static GUIContent label_DstMeshes           = new GUIContent();
            public static GUIContent label_MeshOverwrite       = new GUIContent();
            public static string     label_ExplanationOfMesh;

            public static string     label_TransferContent;
            public static GUIContent label_VtxNmlTransfer      = new GUIContent();
            public static GUIContent label_VtxColorsTransfer   = new GUIContent();
            public static GUIContent label_MoveEachVtx         = new GUIContent();

            public static string     label_SearchMethod;

            public static GUIContent label_BoneWtsTransfer     = new GUIContent();

            public static string     label_ChangeSettings;
            public static GUIContent label_MeshCompress        = new GUIContent();
            public static GUIContent label_ReadableMesh        = new GUIContent();

            public static string     label_ExtraContent;
            public static GUIContent label_StoreAveragedNml    = new GUIContent();
            public static GUIContent label_TypeOfStoreNml      = new GUIContent();
            public static GUIContent label_StoreVtxColor       = new GUIContent();
            public static GUIContent label_TypeOfStoreVtxColor = new GUIContent();

            public static string log_SrcMeshNoColor;
            public static string log_SrcMeshNoBoneWt;

            public static string log_SrcDstNoSet;
            public static string log_SrcNoSet;
            public static string log_DstNoSet;

            public static string log_NoSelectMesh;
            public static string log_NoSelect;
            public static string log_NoHasMesh;

            public static string log_ProgressCancel;
        }

        private static int   selectedLang      = 0;
        private const string SELECTED_LANG_KEY = "TransferAttributesWindow selectedLang";


        private void SetupMessages()
        {
            var subjectTextColor = (EditorGUIUtility.isProSkin == true) ? "<color=silver>" : "<color=black>";

            if (selectedLang == 0)
            {
                Messages.label_SrcMesh.text                =
                    subjectTextColor + "<size=12><b>転送元メッシュ (From)</b></size></color>";
                Messages.label_DstMeshes.text              =
                    subjectTextColor + "<size=12><b>転送先メッシュリスト (To)</b></size></color>";

                Messages.label_ExplanationOfMesh           = 
                    "<color=#606060ff><size=9>※ゲームオブジェクトかメッシュアセットを上記にセットして下さい。</size></color>";

                Messages.label_TransferContent             =
                    subjectTextColor + "<size=12><b>転送する内容</b></size></color>";

                Messages.label_VtxNmlTransfer.text         = "頂点 法線";
                Messages.label_VtxNmlTransfer.tooltip      = "頂点法線を転送しますか？";

                Messages.label_VtxColorsTransfer.text      = "頂点 カラー";
                Messages.label_VtxColorsTransfer.tooltip   = "頂点カラーを転送しますか？";

                Messages.label_MoveEachVtx.text            = "頂点 位置";
                Messages.label_MoveEachVtx.tooltip         = "各頂点を転送元メッシュの最接近点に移動させますか？";

                Messages.label_BoneWtsTransfer.text        = "ボーン ウェイト";
                Messages.label_BoneWtsTransfer.tooltip     = "各頂点のボーンウェイトを転送しますか？";

                Messages.label_ExtraContent                =
                    subjectTextColor + "<size=12><b>その他</b></size></color>";

                Messages.label_StoreAveragedNml.text       = "平均化した法線";
                Messages.label_StoreAveragedNml.tooltip    =
                    "各頂点法線を平均化したものを下のPopupから選択されたUVチャンネルに格納しますか？";

                Messages.label_TypeOfStoreNml.text         =
                    subjectTextColor + "<size=10>格納先 →</size></color>";
                Messages.label_TypeOfStoreNml.tooltip      =
                    "平均化した法線の格納先をTangent,UVチャンネル,Normalから選択して下さい";

                Messages.label_StoreVtxColor.text          = "テクスチャの色を頂点カラーに";
                Messages.label_StoreVtxColor.tooltip       =
                    "各頂点ごとにUV値を元にテクスチャの色を取得し、下のPopupから選択されたUVチャンネルに格納しますか？";

                Messages.label_TypeOfStoreVtxColor.text    =
                    subjectTextColor + "<size=10>格納先 →</size></color>";
                Messages.label_TypeOfStoreVtxColor.tooltip =
                    "テクスチャの色の格納先をVertexColor、UVチャンネルから選択して下さい";

                Messages.label_ChangeSettings              =
                    subjectTextColor + "<size=12><b>メッシュの設定変更</b></size></color>";

                Messages.label_MeshCompress.text           = "メッシュの圧縮";
                Messages.label_MeshCompress.tooltip        =
                    "ModelのImport設定にある Mesh Compression と同じ意味です。";

                Messages.label_ReadableMesh.text           = "メッシュの読み書き";
                Messages.label_ReadableMesh.tooltip        = 
                    "ModelのImport設定にある Read/Write Enabled と同じ意味です。" +
                    "ランタイムでメッシュデータの読み書きしない場合はOFFを推奨";

                Messages.label_MeshOverwrite.text          = "<size=12>出力メッシュデータ上書き</size>";
                Messages.label_MeshOverwrite.tooltip       =
                    "転送後の出力メッシュデータを入力されたメッシュデータに上書きしますか？\n" +
                    "※上書きしない場合は新しいメッシュが作成されます";

                Messages.label_SearchMethod                = "検索方法 →";


                Messages.log_SrcMeshNoColor  =　"頂点カラーのチェックボックスがONになっていましたが、" +
                    "転送元メッシュにカラーがありませんでした";
                Messages.log_SrcMeshNoBoneWt = "ボーンウェイトのチェックボックスがONになっていましたが、" +
                    "転送元メッシュにボーンウェイトがありませんでした";

                Messages.log_SrcDstNoSet     = "転送元メッシュも転送先メッシュもどちらもセットされていません...";
                Messages.log_SrcNoSet        = "転送元メッシュがセットされていません...";
                Messages.log_DstNoSet        = "転送先メッシュが１つもセットされていません...";

                Messages.log_NoSelect        = "何も選択されていません...";
                Messages.log_NoSelectMesh    = "メッシュかゲームオブジェクトを選択して下さい...";
                Messages.log_NoHasMesh       = "選択されたオブジェクトはメッシュを持っていません...";

                Messages.log_ProgressCancel  = "処理がキャンセルされました";

                typeOfSearchMethodDispNames[0] = "平均化した法線に沿った最近接";
                typeOfSearchMethodDispNames[1] = "ポイントに最近接";
            }
            else
            {
                Messages.label_SrcMesh.text                =
                    subjectTextColor + "<size=12><b>Source Mesh (From)</b></size></color>";
                Messages.label_DstMeshes.text              =
                    subjectTextColor + "<size=12><b>Destination Mesh List (To)</b></size></color>";

                Messages.label_ExplanationOfMesh           = 
                    "<color=#606060ff><size=9>※Please set GameObject or Mesh asset in the above.</size></color>";

                Messages.label_TransferContent             =
                    subjectTextColor + "<size=12><b>Transfer Contents</b></size></color>";

                Messages.label_VtxNmlTransfer.text         = "Vertex Normals";
                Messages.label_VtxNmlTransfer.tooltip      = "Do you want to transfer vertex normals?";

                Messages.label_VtxColorsTransfer.text      = "Vertex Colors";
                Messages.label_VtxColorsTransfer.tooltip   = "Do you want to transfer vertex colors?";

                Messages.label_MoveEachVtx.text            = "Vertex Positions";
                Messages.label_MoveEachVtx.tooltip         =
                    "Do you want to move each vertex positions to the closest point in source mesh?";

                Messages.label_BoneWtsTransfer.text        = "Bone Weights";
                Messages.label_BoneWtsTransfer.tooltip     = "Do you want to transfer bone weights?";

                Messages.label_ExtraContent                =
                    subjectTextColor + "<size=12><b>Extra</b></size></color>";

                Messages.label_StoreAveragedNml.text       = "Averaged Normals";
                Messages.label_StoreAveragedNml.tooltip    =
                    "Do you want to store averaged each vertex normal " +
                    "to UV channel selected from the popup below?";

                Messages.label_TypeOfStoreNml.text         =
                    subjectTextColor + "<size=9>Store Dst →</size></color>";
                Messages.label_TypeOfStoreNml.tooltip      =
                    "Select Tangent or UV Channel or Normal that the averaged normals will be stored.";

                Messages.label_StoreVtxColor.text          = "<size=10>Texture Color to Vertex Color</size>";
                Messages.label_StoreVtxColor.tooltip       =
                    "Do you want to store texture color " +
                    "to Vertex Color or UV channel selected from the popup below?";

                Messages.label_TypeOfStoreVtxColor.text    =
                    subjectTextColor + "<size=9>Store Dst →</size></color>";
                Messages.label_TypeOfStoreVtxColor.tooltip =
                    "Select Vertex Color or UV Channel that the texture color will be stored for vertex color.";

                Messages.label_ChangeSettings              =
                    subjectTextColor + "<size=12><b>Change Mesh Settings</b></size></color>";

                Messages.label_MeshCompress.text           = "<size=9>Mesh Compression</size>";
                Messages.label_MeshCompress.tooltip        =
                    "This has the same meaning as 'Mesh Compression' in the Model Importer Settings.";

                Messages.label_ReadableMesh.text           = "<size=9>Read/Write Enabled</size>";
                Messages.label_ReadableMesh.tooltip        = 
                    "This has the same meaning as 'Read/Write Enabled' in the Model Importer Settings." +
                    "If you do not need to read / write mesh data at runtime it is recommended to turn it off.";

                Messages.label_MeshOverwrite.text          = "<size=12>Overwrite Destination Mesh Data</size>";
                Messages.label_MeshOverwrite.tooltip       =
                    "Do you want to overwrite the destination mesh data?\n" +
                    "※ If you do not overwrite mesh data, new mesh create";

                Messages.label_SearchMethod                = "Search Method →";


                Messages.log_SrcMeshNoColor  = "Though The Vertex Colors checkbox was ON," +
                    "the Source Mesh did not have vertex color data.";
                Messages.log_SrcMeshNoBoneWt = "Though The Bone Weights checkbox was ON," +
                    "the Source Mesh did not have bone weights data.";

                Messages.log_SrcDstNoSet     = "Neigher Source Object nor Destination Object is set...";
                Messages.log_SrcNoSet        = "Source Object is not set...";
                Messages.log_DstNoSet        = "Destination Object is not set...";

                Messages.log_NoSelect        = "No Selection...";
                Messages.log_NoSelectMesh    = "Please select Mesh or GameObject...";
                Messages.log_NoHasMesh       = "The selected object has no mesh...";

                Messages.log_ProgressCancel  = "This Process has been canceled";

                typeOfSearchMethodDispNames[0] = "Closet along averaged normal";
                typeOfSearchMethodDispNames[1] = "Closet to point";
            }
        }


        [MenuItem ("Tools/Transfer Attributes")]
        static void ShowWindow()
        {
            window     = EditorWindow.GetWindow<TransferAttributesWindow>();
            windowSize = defaultWindowSize;
            window.maxSize = window.minSize = windowSize;
        }


        void OnEnable()
        {
            toggleMeshOverwrite    = EditorPrefs.GetBool(TOGGLE_MESH_OVERWRITE_KEY ,true);
            toggleVtxColorTransfer = EditorPrefs.GetBool(TOGGLE_VTX_COLOR_TRANSFER_KEY ,true);
            toggleVtxNmlTransfer   = EditorPrefs.GetBool(TOGGLE_VTX_NML_TRANSFER_KEY ,true);
            toggleMoveEachVtx      = EditorPrefs.GetBool(TOGGLE_MOVE_EACH_VTX_KEY ,true);
            toggleBoneWtTransfer   = EditorPrefs.GetBool(TOGGLE_BONE_WT_TRANSFER_KEY ,true);
            toggleStoreAveragedNml = EditorPrefs.GetBool(TOGGLE_STORE_AVERAGED_NML_KEY ,true);
            toggleStoreVtxColor    = EditorPrefs.GetBool(TOGGLE_STORE_VTX_COLOR_KEY ,true);
            toggleMeshCompress     = EditorPrefs.GetBool(TOGGLE_MESH_COMPRESS_KEY ,true);

            typeOfSearchMethod  = (TypeOfSearchMethod)EditorPrefs.GetInt(
                                    TYPE_OF_SEARCH_METHOD, (int)TypeOfSearchMethod.UseAveragedNormal);
            typeOfStoreNml      = (TypeOfStoreNml)EditorPrefs.GetInt(
                                    TYPE_OF_STORE_NML_KEY, (int)TypeOfStoreNml.Tangent);
            typeOfStoreVtxColor = (TypeOfStoreVtxColor)EditorPrefs.GetInt(
                                    TYPE_OF_STORE_VTX_COLOR_KEY, (int)TypeOfStoreVtxColor.Vertex_Color);
            meshCompressOption  = (ModelImporterMeshCompression)EditorPrefs.GetInt(
                                    MESH_COMPRESS_OPTION_KEY, (int)ModelImporterMeshCompression.Off);
            isReadableMesh      = EditorPrefs.GetBool(IS_READABLE_MESH_KEY, false);

            // KEYがない場合 Application.systemLanguage で日本語かどうかを確認して selectedLang を設定
            selectedLang = EditorPrefs.GetInt(
                                SELECTED_LANG_KEY,
                                Application.systemLanguage == SystemLanguage.Japanese ? 0 : 1);
                
            SetupMessages();
        }


        void OnGUI()
        {
            var labelStyle       = new GUIStyle();
            labelStyle.richText  = true;
            labelStyle.font      = GUI.skin.font;
            
            var buttonStyle      = GUI.skin.button;
            buttonStyle.richText = true;
            
            using (new BackgroundColorScope(new Color(0.7f, 0.9f, 1f, 1f)))
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField(Messages.label_SrcMesh, labelStyle, GUILayout.Height(15f));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        srcObj = EditorGUILayout.ObjectField(srcObj, typeof(Object), true);
                        if (EditorGUI.EndChangeCheck())
                        {
                            SetMeshObject("src", 0, srcObj);
                        }

                        var srcSetBtnRect = GUILayoutUtility.GetRect(
                                                10f, 20f, new GUILayoutOption[]{ GUILayout.Width(40f) });

                        using (new BackgroundColorScope(new Color(0.5f, 0.7f, 0.9f, 1f)))
                        {
                            if (GUI.Button(srcSetBtnRect, "SET"))
                            {
                                SetMeshObject("src", 0, null);
                            }
                        }
                    }
                }
            }
            GUILayout.Space(4);

            var dstMeshDataCount = dstMeshDataList.Count;

            using (new BackgroundColorScope(new Color(1f, 0.8f, 0.9f, 1f)))
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope(GUILayout.Height(20f)))
                    {
                        EditorGUILayout.LabelField(Messages.label_DstMeshes, labelStyle, GUILayout.Height(15f));

                        using (new BackgroundColorScope(new Color(0.9f, 0.6f, 0.7f, 1f)))
                        {
                            var symbolBtnRect = GUILayoutUtility.GetRect(
                                                    10f, 20f, new GUILayoutOption[]{ GUILayout.Width(35f) });

                            symbolBtnRect.x -= 30f; //　左に移動
                            if (GUI.Button(symbolBtnRect, "<size=16> + </size>", buttonStyle))
                            {
                                dstMeshDataList.Add(new DstMeshData());
                                dstMeshDataCount++;

                                windowSize.y += 20;
                                window.maxSize = window.minSize = windowSize;
                                this.Repaint();
                            }

                            symbolBtnRect.x += 40f; // +とズラすために右に移動
                            if (GUI.Button(symbolBtnRect, "<size=12> ー </size>", buttonStyle))
                            {
                                if (dstMeshDataCount > 1)
                                {
                                    dstMeshDataList.RemoveAt(dstMeshDataCount - 1);
                                    dstMeshDataCount--;

                                    windowSize.y -= 20;
                                    window.maxSize = window.minSize = windowSize;
                                    this.Repaint();
                                }
                            }

                            var setAllBtnRect = GUILayoutUtility.GetRect(
                                                    10f, 20f, new GUILayoutOption[]{ GUILayout.Width(60f) });
                            setAllBtnRect.x += 25f;

                            if (GUI.Button(setAllBtnRect, "SET All"))
                            {
                                var selObjs = Selection.objects;
                                var addObjListForDstMeshData = new List<Object>();

                                foreach (var selObj in selObjs)
                                {
                                    Mesh selMesh = null;
                                    GameObject selGameObj = null;

                                    if (selObj.GetType() == typeof(UnityEngine.Mesh))
                                    {
                                        selMesh = (Mesh)selObj;
                                    }
                                    else if (selObj.GetType() == typeof(UnityEngine.GameObject))
                                    {
                                        selGameObj = (GameObject)selObj;
                                    }

                                    if (selMesh == null && selGameObj == null) { continue; }

                                    if (selGameObj != null)
                                    {
                                        var mf  = selGameObj.GetComponent<MeshFilter>();
                                        var smr = selGameObj.GetComponent<SkinnedMeshRenderer>();
                                        if (mf == null && smr == null) { continue; }
                                    }
                                    
                                    if (selMesh != null || selGameObj != null)
                                    {
                                        addObjListForDstMeshData.Add(selObj);
                                    }
                                }

                                dstMeshDataList.Clear();
                                var addObjCount = addObjListForDstMeshData.Count;
                                dstMeshDataCount = addObjCount;

                                for (var n = 0; n < addObjCount; n++)
                                {
                                    dstMeshDataList.Add(new DstMeshData());
                                    SetMeshObject("dst", n, addObjListForDstMeshData[n]);
                                }

                                // defaultWindowSize にすでにdstMeshData1つ分の高さが入っているため、
                                // dstMeshDataCountから-1する
                                windowSize.y = defaultWindowSize.y + (20 * (dstMeshDataCount - 1));
                                window.maxSize = window.minSize = windowSize;
                                this.Repaint();
                            }
                        }
                        EditorGUILayout.Space();
                    }

                    for (var m = 0; m < dstMeshDataCount; m++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUI.BeginChangeCheck();
                            dstMeshDataList[m].dstObj = EditorGUILayout.ObjectField(
                                                            dstMeshDataList[m].dstObj,
                                                            typeof(Object),
                                                            true);
                            if (EditorGUI.EndChangeCheck())
                            {
                                SetMeshObject("dst", m, dstMeshDataList[m].dstObj);
                            }

                            var dstSetBtnRect = GUILayoutUtility.GetRect(
                                10f, 20f, new GUILayoutOption[]{ GUILayout.Width(40f) });

                            using (new BackgroundColorScope(new Color(0.9f, 0.6f, 0.7f, 1f)))
                            {
                                if (GUI.Button(dstSetBtnRect, "SET"))
                                {
                                    SetMeshObject("dst", m, null);
                                }
                            }
                        }
                    }
                }
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(Messages.label_ExplanationOfMesh, labelStyle, GUILayout.Height(10f));
            EditorGUI.indentLevel--;

            GUILayout.Space(4f);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2f));
            GUILayout.Space(4f);

            EditorStyles.popup.fontSize    = 11;
            EditorStyles.popup.fixedHeight = 16f;

            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.VerticalScope("Box", GUILayout.Height(56f)))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(
                            Messages.label_TransferContent,
                            labelStyle,
                            GUILayout.Width(130f),
                            GUILayout.Height(14f));

                        using (new EditorGUILayout.HorizontalScope("Box"))
                        {
                            EditorGUIUtility.labelWidth = (selectedLang == 0) ? 65f : 105f;

                            var rect = EditorGUILayout.GetControlRect();

                            EditorGUI.BeginChangeCheck();
                            typeOfSearchMethod = (TypeOfSearchMethod)EditorGUI.Popup(
                                                    rect,
                                                    Messages.label_SearchMethod,
                                                    (int)typeOfSearchMethod,
                                                    typeOfSearchMethodDispNames);
                            if (EditorGUI.EndChangeCheck())
                            {
                                EditorPrefs.SetInt(TYPE_OF_SEARCH_METHOD, (int)typeOfSearchMethod);
                            }
                        }
                    }

                    EditorGUI.indentLevel++;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        toggleVtxColorTransfer = EditorGUILayout.ToggleLeft(
                                                    Messages.label_VtxColorsTransfer,
                                                    toggleVtxColorTransfer,
                                                    GUILayout.Width(170f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorPrefs.SetBool(TOGGLE_VTX_COLOR_TRANSFER_KEY, toggleVtxColorTransfer);
                        }

                        EditorGUI.BeginChangeCheck();
                        toggleVtxNmlTransfer = EditorGUILayout.ToggleLeft(
                                                    Messages.label_VtxNmlTransfer,
                                                    toggleVtxNmlTransfer,
                                                    GUILayout.Width(170f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorPrefs.SetBool(TOGGLE_VTX_NML_TRANSFER_KEY, toggleVtxNmlTransfer);
                            if (toggleVtxNmlTransfer == true && typeOfStoreNml == TypeOfStoreNml.Normal)
                            {
                                typeOfStoreNml = TypeOfStoreNml.Tangent;
                            }
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {    
                        EditorGUI.BeginChangeCheck();
                        toggleMoveEachVtx = EditorGUILayout.ToggleLeft(
                                                Messages.label_MoveEachVtx,
                                                toggleMoveEachVtx,
                                                GUILayout.Width(170f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorPrefs.SetBool(TOGGLE_MOVE_EACH_VTX_KEY, toggleMoveEachVtx);
                        }

                        EditorGUI.BeginChangeCheck();
                        toggleBoneWtTransfer = EditorGUILayout.ToggleLeft(
                                                    Messages.label_BoneWtsTransfer,
                                                    toggleBoneWtTransfer,
                                                    GUILayout.Width(170f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorPrefs.SetBool(TOGGLE_BONE_WT_TRANSFER_KEY, toggleBoneWtTransfer);
                        }
                    }

                    EditorGUI.indentLevel--;
                }

                using (new EditorGUILayout.VerticalScope("Box", GUILayout.Height(85f)))
                {
                    EditorGUILayout.LabelField(Messages.label_ExtraContent, labelStyle, GUILayout.Height(14f));

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUILayout.VerticalScope("Box", GUILayout.Height(60f)))
                        {
                            EditorGUI.BeginChangeCheck();
                            toggleStoreAveragedNml = EditorGUILayout.ToggleLeft(
                                                        Messages.label_StoreAveragedNml,
                                                        toggleStoreAveragedNml,
                                                        GUILayout.Width(178f));
                            if (EditorGUI.EndChangeCheck())
                            {
                                EditorPrefs.SetBool(TOGGLE_STORE_AVERAGED_NML_KEY, toggleStoreAveragedNml);
                            }

                            using (new EditorGUI.DisabledGroupScope(toggleStoreAveragedNml == false))
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    var storeNmlPopupWidth = (selectedLang == 0) ? 46f : 60f;

                                    EditorGUI.indentLevel++;

                                    EditorGUILayout.LabelField(
                                        Messages.label_TypeOfStoreNml,
                                        labelStyle,
                                        GUILayout.Width(storeNmlPopupWidth));
                                    
                                    EditorGUI.BeginChangeCheck();
                                    typeOfStoreNml = (TypeOfStoreNml)EditorGUILayout.EnumPopup(
                                                        typeOfStoreNml,
                                                        GUILayout.Width(178f - storeNmlPopupWidth));
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        EditorPrefs.SetInt(TYPE_OF_STORE_NML_KEY, (int)typeOfStoreNml);

                                        if (typeOfStoreNml == TypeOfStoreNml.Normal
                                        &&  toggleVtxNmlTransfer == true)
                                        {
                                            toggleVtxNmlTransfer = false;
                                        }
                                    }
                                    EditorGUI.indentLevel--;
                                }
                            }
                        }

                        using (new EditorGUILayout.VerticalScope("Box", GUILayout.Height(60f)))
                        {
                            EditorGUI.BeginChangeCheck();
                            toggleStoreVtxColor = EditorGUILayout.ToggleLeft(
                                                    Messages.label_StoreVtxColor,
                                                    toggleStoreVtxColor,
                                                    labelStyle,
                                                    GUILayout.Width(178f));
                            if (EditorGUI.EndChangeCheck())
                            {
                                EditorPrefs.SetBool(TOGGLE_STORE_VTX_COLOR_KEY, toggleStoreVtxColor);
                            }

                            using (new EditorGUI.DisabledGroupScope(toggleStoreVtxColor == false))
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    var storeVtxColorPopupWidth = (selectedLang == 0) ? 46f : 60f;

                                    EditorGUI.indentLevel++;

                                    EditorGUILayout.LabelField(
                                        Messages.label_TypeOfStoreVtxColor,
                                        labelStyle,
                                        GUILayout.Width(storeVtxColorPopupWidth));

                                    EditorGUI.BeginChangeCheck();
                                    typeOfStoreVtxColor = (TypeOfStoreVtxColor)EditorGUILayout.EnumPopup(
                                                            typeOfStoreVtxColor,
                                                            GUILayout.Width(178f - storeVtxColorPopupWidth));
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        EditorPrefs.SetInt(TYPE_OF_STORE_VTX_COLOR_KEY, (int)typeOfStoreVtxColor);

                                        if (typeOfStoreVtxColor == TypeOfStoreVtxColor.Vertex_Color
                                        && toggleVtxColorTransfer == true)
                                        {
                                            toggleVtxColorTransfer = false;
                                        }
                                    }
                                    EditorGUI.indentLevel--;
                                }

                                EditorGUI.indentLevel++;

                                EditorGUI.BeginChangeCheck();
                                texOfStoreVtxColor = (Texture2D)EditorGUILayout.ObjectField(
                                                        texOfStoreVtxColor,
                                                        typeof(Texture2D),
                                                        true,
                                                        GUILayout.Width(182f));
                                if (EditorGUI.EndChangeCheck())
                                {
                                    var texInstanceID   = texOfStoreVtxColor.GetInstanceID();
                                    var srcColorTexPath = AssetDatabase.GetAssetPath(texInstanceID);
                                    // Debug.Log("srcColorTexPath = " + srcColorTexPath);
                                    texOfStoreVtxColorImporter = (TextureImporter)AssetImporter.GetAtPath(srcColorTexPath);
                                    texOfStoreVtxColorReadable = texOfStoreVtxColorImporter.isReadable;
                                }

                                EditorGUI.indentLevel--;
                                
                            }
                        }
                    }
                }
                
                using (new EditorGUILayout.VerticalScope("Box", GUILayout.Height(46f)))
                {
                    EditorGUILayout.LabelField(Messages.label_ChangeSettings, labelStyle, GUILayout.Height(14f));

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUILayout.HorizontalScope("Box", GUILayout.Height(24f)))
                        {
                            var meshCompressWidth = (selectedLang == 0) ? 82f : 102f;

                            EditorGUI.BeginChangeCheck();
                            toggleMeshCompress = EditorGUILayout.ToggleLeft(
                                                    Messages.label_MeshCompress,
                                                    toggleMeshCompress,
                                                    labelStyle,
                                                    GUILayout.Width(meshCompressWidth));
                            if (EditorGUI.EndChangeCheck())
                            {
                                EditorPrefs.SetBool(TOGGLE_MESH_COMPRESS_KEY, toggleMeshCompress);
                            }

                            using (new EditorGUI.DisabledGroupScope(toggleMeshCompress == false))
                            {
                                EditorGUI.indentLevel++;

                                EditorGUI.BeginChangeCheck();
                                meshCompressOption = (ModelImporterMeshCompression)EditorGUILayout.EnumPopup(
                                                        meshCompressOption,
                                                        GUILayout.Width(178f - meshCompressWidth));
                                if (EditorGUI.EndChangeCheck())
                                {
                                    EditorPrefs.SetInt(MESH_COMPRESS_OPTION_KEY, (int)meshCompressOption);
                                }

                                EditorGUI.indentLevel--;
                            }
                        }

                        using (new EditorGUILayout.HorizontalScope("Box", GUILayout.Height(24f)))
                        {
                            EditorGUI.BeginChangeCheck();
                            toggleReadableMesh = EditorGUILayout.ToggleLeft(
                                                    Messages.label_ReadableMesh,
                                                    toggleReadableMesh,
                                                    labelStyle,
                                                    GUILayout.Width(118f));
                            if (EditorGUI.EndChangeCheck())
                            {
                                EditorPrefs.SetBool(TOGGLE_READABLE_MESH_KEY, toggleReadableMesh);
                            }

                            using (new EditorGUI.DisabledGroupScope(toggleReadableMesh == false))
                            {
                                EditorGUI.indentLevel++;

                                EditorGUI.BeginChangeCheck();
                                var isReadable = GUILayout.Toolbar(
                                                    isReadableMesh ? 1 : 0,
                                                    new string[]{ "ON", "OFF" },
                                                    GUILayout.Height(16f),
                                                    GUILayout.Width(60f));
                                if (EditorGUI.EndChangeCheck())
                                {
                                    isReadableMesh = (isReadable == 1) ? true : false;
                                    EditorPrefs.SetBool(IS_READABLE_MESH_KEY, isReadableMesh);
                                }

                                EditorGUI.indentLevel--;
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            
            GUILayout.Space(8f);

            using (new EditorGUILayout.HorizontalScope())
            {
                var meshOverwriteWidth = (selectedLang == 0) ? 100f : 170f;

                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();
                toggleMeshOverwrite = EditorGUILayout.ToggleLeft(
                                        Messages.label_MeshOverwrite,
                                        toggleMeshOverwrite,
                                        labelStyle,
                                        GUILayout.Width(meshOverwriteWidth));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetBool(TOGGLE_MESH_OVERWRITE_KEY, toggleMeshOverwrite);
                }
                EditorGUILayout.Space();
            }

            GUILayout.Space(8f);

            // using (new EditorGUILayout.HorizontalScope())
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.Space();
                if (GUILayout.Button(
                    "<size=14><b>Transfer Attributes</b></size>",
                    buttonStyle,
                    GUILayout.Width(180f),
                    GUILayout.Height(40f)))
                {
                    try
                    {
                        TransferAttributes();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e);
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                        progress         = 0f;
                        progressInfo     = "";
                        isProgressCancel = false;

                        Debug.Log("finally   ===================================");

                        // toggleStoreVtxColor == true 且つ texToStoreVtxColorReadable == false の場合、
                        // セットしたテクスチャのRead/Write Enable(isReadable)をtrueに変更している可能性があるので、
                        // Read/Write Enableをfalseに戻しておく
                        if (toggleStoreVtxColor == true
                        &&  texOfStoreVtxColorReadable == false
                        &&  texOfStoreVtxColor != null)
                        {
                            Debug.Log("ChangeTextureReadWriteEnable : Falseにもどす");
                            TAP.ChangeReadWriteEnableInTexture(
                                texOfStoreVtxColor,
                                texOfStoreVtxColorImporter,
                                false);
                        }
                    }
                    return;
                }

                EditorGUILayout.Space();

                if (GUILayout.Button(
                    "<size=14><b>Test</b></size>",
                    buttonStyle,
                    GUILayout.Width(100f),
                    GUILayout.Height(40f)))
                {
                    try
                    {
                        Test();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e);
                    }
                    finally
                    {
                    }
                    return;
                    
                }
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4f);


            // using (new EditorGUILayout.HorizontalScope())
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();
                selectedLang = GUILayout.Toolbar(
                                selectedLang,
                                new string[]{ "jp", "en" },
                                GUILayout.Width(60f));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetInt(SELECTED_LANG_KEY, selectedLang);
                    SetupMessages();
                }

                GUILayout.Space(4f);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void Test()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            
            var dstMeshData = dstMeshDataList[0];
            Dictionary<Vector3, List<int>> dstVtxPosIdxDict;
            Dictionary<Vector3, List<int>> dstVtxPosTriangleIdxDict;
            List<TAP.TriangleData> dstTriangleDataList;
            TAP.GetTriangleDataList(
                dstMeshData.dstMesh,
                dstMeshData.dstMF,
                dstMeshData.dstSMR,
                true,
                true,
                out dstVtxPosIdxDict,
                out dstVtxPosTriangleIdxDict,
                out dstTriangleDataList);

            var bakedTex = TAP.BakeVtxColToTexture(
                            dstMeshData.dstMesh,
                            dstTriangleDataList,
                            512,
                            512,
                            TextureFormat.ARGB32,
                            false,
                            typeOfUseUV);

            var exportFullPath = AssetDatabase.GetAssetPath(dstMeshData.dstMesh.GetInstanceID());
            Debug.Log("exportFullPath = " + exportFullPath + "  :  InstanceID = " + dstMeshData.dstMesh.GetInstanceID());
            var exportPath     = System.IO.Path.GetDirectoryName(exportFullPath);
            Debug.Log("exportPath = " + exportPath);
            var newFullName    = AssetDatabase.GenerateUniqueAssetPath(exportPath + "/Textures/bakedTexture.png");

            Debug.Log("newFullName = " + newFullName);
            // AssetDatabase.CreateAsset(bakedTex, newFullName);
            System.IO.File.WriteAllBytes(newFullName, bakedTex.EncodeToPNG());
        
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Object.DestroyImmediate(bakedTex);

            // 計測停止
            sw.Stop();

            // 結果表示
            Debug.Log("■処理Aにかかった時間");
            TimeSpan ts = sw.Elapsed;
            Debug.Log(ts);
            Debug.Log(ts.Hours + "時間 : " + ts.Minutes + "分 : " + ts.Seconds + "秒 : " + ts.Milliseconds + "ミリ秒");
            Debug.Log("経過 " + sw.ElapsedMilliseconds + " ミリ秒");

            // AssetDatabase.ImportAsset(newFullName);
        }

        /// <summary>
        /// Set the object passed as argumant to the target mesh.
        /// </summary>
        /// <param name="targetKind"></param>
        /// <param name="index"></param>
        /// <param name="setObj"></param>
        private void SetMeshObject(string targetKind, int index, Object setObj)
        {
            Mesh curMesh               = null;
            MeshFilter curMF           = null;
            SkinnedMeshRenderer curSMR = null;
            Object curObj              = null;
            GameObject curGameObj      = null;

            if (setObj != null)
            {
                curObj = setObj;
                
                if (curObj.GetType() == typeof(UnityEngine.Mesh))
                {
                    curMesh = (Mesh)curObj;
                }
                else if (curObj.GetType() == typeof(UnityEngine.GameObject))
                {
                    curGameObj = (GameObject)curObj;
                }
                else
                {
                    Debug.LogWarning(Messages.log_NoSelectMesh);
                    EditorUtility.DisplayDialog("TransferAttributes Warning", Messages.log_NoSelectMesh, "OK");
                    return;
                }
            }
            else
            {
                curObj     = Selection.activeObject;
                curGameObj = Selection.activeGameObject;

                if (curObj == null && curGameObj == null)
                {
                    Debug.LogWarning(Messages.log_NoSelect);
                    EditorUtility.DisplayDialog("TransferAttributes Warning", Messages.log_NoSelect, "OK");
                    return;
                }
            }

            if (curGameObj != null)
            {
                curMF  = curGameObj.GetComponentInChildren<MeshFilter>();
                curSMR = curGameObj.GetComponentInChildren<SkinnedMeshRenderer>();

                if (curMF != null)
                {
                    curMesh = curMF.sharedMesh;
                }
                else if (curSMR != null)
                {
                    curMesh = curSMR.sharedMesh;
                }
            }
            else if (curObj != null && curObj.GetType() == typeof(UnityEngine.Mesh))
            {
                curMesh = (Mesh)curObj;
            }

            if (curMesh == null)
            {
                Debug.LogWarning(Messages.log_NoHasMesh);
                EditorUtility.DisplayDialog("TransferAttributes Warning", Messages.log_NoHasMesh, "OK");
                return;
            }

            if (targetKind == "src")
            {
                srcObj     = curObj; 
                srcMesh    = curMesh;
                srcGameObj = curGameObj;

                if (curMF != null)
                {
                    srcMF    = curMF;
                    srcSMR   = null;
                    srcTrans = curMF.transform;
                }
                else if (curSMR != null)
                {
                    srcMF    = null;
                    srcSMR   = curSMR;
                    srcTrans = curSMR.transform;
                }
                else
                {
                    srcMF    = null;
                    srcSMR   = null;
                    srcTrans = null;
                }
            }
            else
            {
                dstMeshDataList[index].dstObj     = curObj;
                dstMeshDataList[index].dstMesh    = curMesh;
                dstMeshDataList[index].dstGameObj = curGameObj;

                if (curMF != null)
                {
                    dstMeshDataList[index].dstMF  = curMF;
                    dstMeshDataList[index].dstSMR = null;
                }
                else if (curSMR != null)
                {
                    dstMeshDataList[index].dstMF  = null;
                    dstMeshDataList[index].dstSMR = curSMR;
                }
                else
                {
                    dstMeshDataList[index].dstMF  = null;
                    dstMeshDataList[index].dstSMR = null;
                }
            }
        }


        private void TransferAttributes()
        {
            isProgressCancel = false;

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            if (srcGameObj == null && dstMeshDataList[0].dstGameObj == null)
            {
                Debug.LogWarning(Messages.log_SrcDstNoSet);
                EditorUtility.DisplayDialog("TransferAttributes Warning", Messages.log_SrcDstNoSet, "OK");
                return;
            }
            else if (srcGameObj == null)
            {
                Debug.LogWarning(Messages.log_SrcNoSet);
                EditorUtility.DisplayDialog("TransferAttributes Warning", Messages.log_SrcNoSet, "OK");
                return;
            }
            else if (dstMeshDataList[0].dstGameObj == null)
            {
                Debug.LogWarning(Messages.log_DstNoSet);
                EditorUtility.DisplayDialog("TransferAttributes Warning", Messages.log_DstNoSet, "OK");
                return;
            }

            // toggleStoreVtxColorがtrueで、
            // セットされたTextureのRead/Write Enabled（isReadable）がfalseの場合、
            // TextureのColorを取得できないので一時的にtrueに変更する
            if (toggleStoreVtxColor == true
            &&  texOfStoreVtxColorReadable == false
            &&  texOfStoreVtxColor != null)
            {
                TAP.ChangeReadWriteEnableInTexture(texOfStoreVtxColor, texOfStoreVtxColorImporter, true);
            }

            // SrcMeshに頂点カラー情報があるかどうかの変数
            var hasSrcVtxColors = srcMesh.colors.Length > 0;
            // toggleVtxColorsTransfer がONでも、SrcMeshの頂点カラー情報がなかったらOFFにする
            if (toggleVtxColorTransfer == true && hasSrcVtxColors == false)
            {
                toggleVtxColorTransfer = false;
                Debug.LogWarning(Messages.log_SrcMeshNoColor);
            }

            // SrcMeshに頂点カラー情報があるかどうかの変数
            var hasSrcBoneWts = srcMesh.boneWeights.Length > 0;
            // toggleBoneWtsTransfer がONでも、SrcMeshのボーンウェイト情報がなかったらOFFにする
            if (toggleBoneWtTransfer == true && hasSrcBoneWts == false)
            {
                toggleBoneWtTransfer = false;
                Debug.LogWarning(Messages.log_SrcMeshNoBoneWt);
            }

            Dictionary<Vector3, List<int>> srcVtxPosIdxDict;
            Dictionary<Vector3, List<int>> srcVtxPosTriangleIdxDict;
            List<TAP.TriangleData> srcTriangleDataList;
            TAP.GetTriangleDataList(
                srcMesh,
                srcMF,
                srcSMR,
                hasSrcVtxColors,
                hasSrcBoneWts,
                out srcVtxPosIdxDict,
                out srcVtxPosTriangleIdxDict,
                out srcTriangleDataList);

            if (isProgressCancel == true)
            {
                EditorUtility.DisplayDialog("TransferAttributes Canceled", Messages.log_ProgressCancel, "OK");
                return;
            }
            

            Dictionary<Vector3, List<Vector3>> srcBelongingSpaceOfTriangleDict = null;
            var srcMeshBounds = srcMesh.bounds;
            Vector3 srcMeshBoundsMax;
            Vector3 srcMeshBoundsMin;
            if (srcTrans != null)
            {
                srcMeshBoundsMax = Vector3.Scale(srcMeshBounds.max, srcTrans.lossyScale);
                srcMeshBoundsMin = Vector3.Scale(srcMeshBounds.min, srcTrans.lossyScale);
            }
            else
            {
                srcMeshBoundsMax = srcMeshBounds.max;
                srcMeshBoundsMin = srcMeshBounds.min;
            }


            var dstMeshCount = dstMeshDataList.Count;

            for (var a = 0; a < dstMeshCount; a++)
            {
                if (dstMeshDataList[a].dstMesh == null) { continue; }

                var instancedDstObj   = (GameObject)Instantiate(dstMeshDataList[a].dstGameObj);
                Mesh instancedDstMesh = null;

                var isSkinnedMeshRenderer = false;
                if (dstMeshDataList[a].dstMF != null)
                {
                    var mf = instancedDstObj.GetComponent<MeshFilter>();
                    instancedDstMesh = (Mesh)Instantiate(mf.sharedMesh);
                    mf.sharedMesh = instancedDstMesh;
                }
                else
                {
                    var smr = instancedDstObj.GetComponent<SkinnedMeshRenderer>();
                    instancedDstMesh = (Mesh)Instantiate(smr.sharedMesh);
                    smr.sharedMesh = instancedDstMesh;
                    isSkinnedMeshRenderer = true;
                }

                var instancedDstVertices_old  = instancedDstMesh.vertices;
                var instancedDstTriangles     = instancedDstMesh.triangles;

                var instancedDstTriangleCount = instancedDstTriangles.Length;

                if (instancedDstTriangleCount > 65000)
                {
    #if UNITY_2017_3_OR_NEWER
                    instancedDstMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    #else
                    Debug.LogWarning(instancedDstMesh.name + " の頂点数が65000を超えていたのでパスしました");
                    continue;
    #endif
                }

                var instancedDstVertices     = new Vector3[instancedDstTriangleCount];

                var instancedDstNormals_old  = instancedDstMesh.normals;
                var instancedDstNormals      = new Vector3[instancedDstTriangleCount];

                var instancedDstTangents_old = instancedDstMesh.tangents;
                var instancedDstTangents     = new Vector4[instancedDstTriangleCount];

                var instancedDstUvs1_old     = instancedDstMesh.uv;
                var instancedDstUvs1         = new Vector2[instancedDstTriangleCount];
                var instancedDstUvs2_old     = instancedDstMesh.uv2;
                var instancedDstUvs2         = new Vector2[instancedDstTriangleCount];
                var instancedDstUvs3_old     = instancedDstMesh.uv3;
                var instancedDstUvs3         = new Vector2[instancedDstTriangleCount];
                var instancedDstUvs4_old     = instancedDstMesh.uv4;
                var instancedDstUvs4         = new Vector2[instancedDstTriangleCount];

                var instancedDstColors_old   = instancedDstMesh.colors;
                var instancedDstColors       = new Color[instancedDstTriangleCount];

                var instancedDstBoneWts_old  = instancedDstMesh.boneWeights;
                var instancedDstBoneWts      = new BoneWeight[instancedDstTriangleCount];

                // この工程でマージされている頂点も一旦バラバラにして各頂点ごとに処理できるようにする
                for (var b = 0; b < instancedDstTriangleCount; b++)
                {
                    instancedDstVertices[b] = instancedDstVertices_old[instancedDstTriangles[b]];

                    if (instancedDstNormals_old.Length > 0)
                    {
                        instancedDstNormals[b] = instancedDstNormals_old[instancedDstTriangles[b]];
                    }

                    if (instancedDstTangents_old.Length > 0)
                    {
                        instancedDstTangents[b] = instancedDstTangents_old[instancedDstTriangles[b]];
                    }

                    if (instancedDstUvs1_old.Length > 0)
                    {
                        instancedDstUvs1[b] = instancedDstUvs1_old[instancedDstTriangles[b]];
                    }

                    if (instancedDstUvs2_old.Length > 0)
                    {
                        instancedDstUvs2[b] = instancedDstUvs2_old[instancedDstTriangles[b]];
                    }

                    if (instancedDstUvs3_old.Length > 0)
                    {
                        instancedDstUvs3[b] = instancedDstUvs3_old[instancedDstTriangles[b]];
                    }

                    if (instancedDstUvs4_old.Length > 0)
                    {
                        instancedDstUvs4[b] = instancedDstUvs4_old[instancedDstTriangles[b]];
                    }

                    if (toggleVtxColorTransfer == true || toggleStoreVtxColor == true)
                    {
                        // instancedDstColors_oldに情報がなかった場合、とりあえずColor.whiteで頂点カラー情報を埋める
                        if (instancedDstColors_old.Length == 0)
                        {
                            instancedDstColors[b] = Color.white;
                        }
                        else
                        {
                            instancedDstColors[b] = instancedDstColors_old[instancedDstTriangles[b]];
                        }
                    }

                    if (toggleBoneWtTransfer)
                    {
                        // DstMeshがSkinnedMeshRendererじゃないか、ボーンウェイトを持ってない場合は仮の値を入れる
                        if (isSkinnedMeshRenderer == false || instancedDstBoneWts_old.Length == 0)
                        {
                            instancedDstBoneWts[b] = new BoneWeight();
                            instancedDstBoneWts[b].boneIndex0 = 0;
                            instancedDstBoneWts[b].weight0    = 1f;
                        }
                        else
                        {
                            instancedDstBoneWts[b] = instancedDstBoneWts_old[instancedDstTriangles[b]];
                        }
                    }
                    
                    instancedDstTriangles[b] = b;
                }
                
                instancedDstMesh.vertices  = instancedDstVertices;
                instancedDstMesh.triangles = instancedDstTriangles;

                if (instancedDstNormals_old.Length > 0)
                {
                    instancedDstMesh.normals = instancedDstNormals;
                }

                if (instancedDstTangents_old.Length > 0)
                {
                    instancedDstMesh.tangents = instancedDstTangents;
                }

                if (instancedDstUvs1_old.Length > 0)
                {
                    instancedDstMesh.uv = instancedDstUvs1;
                }

                if (instancedDstUvs2_old.Length > 0)
                {
                    instancedDstMesh.uv2 = instancedDstUvs2;
                }

                if (instancedDstUvs3_old.Length > 0)
                {
                    instancedDstMesh.uv3 = instancedDstUvs3;
                }

                if (instancedDstUvs4_old.Length > 0)
                {
                    instancedDstMesh.uv4 = instancedDstUvs4;
                }

                if (toggleVtxColorTransfer == true || toggleStoreVtxColor == true)
                {
                    instancedDstMesh.colors = instancedDstColors;
                }

                if (toggleBoneWtTransfer)
                {
                    instancedDstMesh.boneWeights = instancedDstBoneWts;
                }

                var newFullName = "";

                var exportPath = AssetDatabase.GetAssetPath(dstMeshDataList[a].dstMesh.GetInstanceID());

                if (exportPath == "" || exportPath == "Library/unity default resources")
                {
                    exportPath = "Assets/";
                }
                else
                {
                    exportPath = System.IO.Path.GetDirectoryName(exportPath) + "/";
                }


                if (toggleMeshOverwrite)
                {
                    newFullName = exportPath + instancedDstObj.name + ".asset";
                }
                else
                {
                    var tmpNewFullName = exportPath + instancedDstObj.name + "_transferedNormal.asset";
                    newFullName = AssetDatabase.GenerateUniqueAssetPath(tmpNewFullName);
                }

                var dstTriangles = instancedDstMesh.triangles;
                var dstVertices  = instancedDstMesh.vertices;

                Transform dstTrans;
                if (dstMeshDataList[a].dstMF != null)
                {
                    dstTrans = dstMeshDataList[a].dstMF.transform;
                }
                else if (dstMeshDataList[a].dstSMR != null)
                {
                    dstTrans = dstMeshDataList[a].dstSMR.transform;
                }
                else
                {
                    dstTrans = null;
                }

                if (typeOfSearchMethod == TypeOfSearchMethod.UseAveragedNormal)
                {
                    // DstMeshのもつノーマルとは別に再計算した法線も使用するのでRecalculateNormalsを実行しておく
                    instancedDstMesh.RecalculateNormals();
                    instancedDstMesh.RecalculateTangents();
                }

                var hasDstVtxColors = instancedDstMesh.colors.Length > 0;
                var hasDstBoneWts   = instancedDstMesh.boneWeights.Length > 0;

                Dictionary<Vector3, List<int>> dstVtxPosIdxDict;
                Dictionary<Vector3, List<int>> dstVtxPosTriangleIdxDict;
                List<TAP.TriangleData> dstTriangleDataList;

                TAP.GetTriangleDataList(
                    instancedDstMesh,
                    dstMeshDataList[a].dstMF,
                    dstMeshDataList[a].dstSMR,
                    hasDstVtxColors,
                    hasDstBoneWts,
                    out dstVtxPosIdxDict,
                    out dstVtxPosTriangleIdxDict,
                    out dstTriangleDataList);

                if (toggleReadableMesh)
                {
                    instancedDstMesh.UploadMeshData(isReadableMesh);
                }

                Dictionary<Vector3, List<Vector3>> dstBelongingSpaceOfTriangleDict = null;
                var dstMeshBounds = instancedDstMesh.bounds;
                Vector3 dstMeshBoundsMax;
                Vector3 dstMeshBoundsMin;
                if (dstTrans != null)
                {
                    dstMeshBoundsMax = Vector3.Scale(dstMeshBounds.max, dstTrans.lossyScale);
                    dstMeshBoundsMin = Vector3.Scale(dstMeshBounds.min, dstTrans.lossyScale);
                }
                else
                {
                    dstMeshBoundsMax = dstMeshBounds.max;
                    dstMeshBoundsMin = dstMeshBounds.min;
                }
                
                TAP.RegisterBelongingSpaceOfVtxPosInMeshes(
                    srcMeshBoundsMax,
                    srcMeshBoundsMin,
                    srcTriangleDataList,
                    ref srcBelongingSpaceOfTriangleDict,
                    dstMeshBoundsMax,
                    dstMeshBoundsMin,
                    dstTriangleDataList,
                    ref dstBelongingSpaceOfTriangleDict,
                    ref voxelSpaceSize,
                    ref voxelSpaceDict);
                

                if ((toggleStoreAveragedNml == true || toggleMoveEachVtx == true) &&  isProgressCancel == false)
                {
                    Debug.LogWarning("ApplyAttributesToMesh ==============");
                    ApplyAttributesToMesh(
                        ref instancedDstMesh,
                        dstTrans,
                        dstVtxPosIdxDict,
                        dstTriangleDataList,
                        srcTriangleDataList,
                        srcVtxPosTriangleIdxDict,
                        srcBelongingSpaceOfTriangleDict,
                        dstBelongingSpaceOfTriangleDict);
                }

                if (toggleStoreVtxColor == true && texOfStoreVtxColor != null && isProgressCancel == false)
                {
                    Debug.LogWarning("ApplyTextureColorToVertexColor ==============");
                    ApplyTextureColorToVertexColor(ref instancedDstMesh, texOfStoreVtxColor);
                }

                if (isProgressCancel == true)
                {
                    EditorUtility.DisplayDialog("TransferAttributes Canceled", Messages.log_ProgressCancel, "OK");
                    return;
                }

                // toggleMoveEachVtx がONだった場合、モデルの形状が変わるため最後にBoundsを計算し直す
                instancedDstMesh.RecalculateBounds();

                if(toggleBoneWtTransfer == true && hasSrcBoneWts == true && hasDstBoneWts == true)
                {
                    instancedDstMesh.bindposes = srcMesh.bindposes;
                }

                // toggleMeshCompress がONだった場合、MeshCompressionを変更する
                if (toggleMeshCompress)
                {
                    MeshUtility.SetMeshCompression(instancedDstMesh, meshCompressOption);
                }

                // toggleReadableMesh がONだった場合、メッシュのRead/Write Enabledを変更する
                if (toggleReadableMesh)
                {
                    instancedDstMesh.UploadMeshData(isReadableMesh);
                }

                // 最後にMeshを最適化する
                MeshUtility.Optimize(instancedDstMesh);

                // 念の為 isProgressCancel がfalseのままか確認してからCreateAssetする
                if (isProgressCancel == false)
                {
                    AssetDatabase.CreateAsset(instancedDstMesh, newFullName);
                    instancedDstObj.transform.localPosition = dstTrans.position;
                    instancedDstObj.transform.localRotation = dstTrans.rotation;
                    instancedDstObj.transform.localScale    = dstTrans.lossyScale;
                }
                else // 
                {
                    GameObject.DestroyImmediate(instancedDstObj);
                }
            }

            AssetDatabase.SaveAssets();
            // 計測停止
            sw.Stop();

            // 結果表示
            Debug.Log("■処理Aにかかった時間");
            TimeSpan ts = sw.Elapsed;
            Debug.Log(ts);
            Debug.Log(ts.Hours + "時間 : " + ts.Minutes + "分 : " + ts.Seconds + "秒 : " + ts.Milliseconds + "ミリ秒");
            Debug.Log("経過 " + sw.ElapsedMilliseconds + " ミリ秒");
        }


        /// <summary>
        /// Store the averaged normals in the texcoord of the selected UV from GUI.
        /// </summary>
        /// <param name="dstMesh"></param>
        /// <param name="dstTrans"></param>
        /// <param name="dstVtxPosIdxDict"></param>
        /// <param name="dstTriangleDataList"></param>
        /// <param name="srcTriangleDataList"></param>
        private static void ApplyAttributesToMesh(
            ref Mesh dstMesh,
            Transform dstTrans,
            Dictionary<Vector3, List<int>> dstVtxPosIdxDict,
            List<TAP.TriangleData> dstTriangleDataList,
            List<TAP.TriangleData> srcTriangleDataList,
            Dictionary<Vector3, List<int>> srcVtxPosTriangleIdxDict,
            Dictionary<Vector3, List<Vector3>> srcBelongingSpaceOfVtxPosDict,
            Dictionary<Vector3, List<Vector3>> dstBelongingSpaceOfVtxPosDict)
        {
            var vtxCount = dstMesh.vertexCount;
            var vertices = dstMesh.vertices;

            var vtxColors = dstMesh.colors;
            if (toggleVtxColorTransfer == true && vtxColors.Length == 0)
            {
                vtxColors = new Color[vtxCount];
            }

            var vtxNmls = dstMesh.normals;
            if (toggleVtxNmlTransfer == true && vtxNmls.Length == 0)
            {
                vtxNmls = new Vector3[vtxCount];
            }

            var boneWts = dstMesh.boneWeights;
            if (toggleBoneWtTransfer == true && boneWts.Length == 0)
            {
                boneWts = new BoneWeight[vtxCount];
            }

            var averagedNmlArray = vtxNmls;

            var loopCount = 0;
            var dictCount = dstVtxPosIdxDict.Count;

            foreach (var dstVtxPosIdxPair in dstVtxPosIdxDict)
            {
                progress = (float)loopCount / dictCount;
                progressInfo = "Apply Attributes To Mesh - " + dstMesh.name + " : " +
                                Mathf.Round(progress * 100).ToString() +
                                "% ( " + loopCount.ToString() + " / " + dictCount.ToString() + " )";

                isProgressCancel = EditorUtility.DisplayCancelableProgressBar(
                                    "Transfer Attributes",
                                    progressInfo,
                                    progress);
                // キャンセルされたら強制return
                if (isProgressCancel == true)
                {
                    Debug.LogWarning("ApplyAttributesToMesh_1  :  キャンセルされました");
                    return;
                }

                var tmpAveragedNml = Vector3.zero;

                if (typeOfSearchMethod == TypeOfSearchMethod.UseAveragedNormal)
                {
                    foreach (var dstVtxIdx in dstVtxPosIdxPair.Value)
                    {
                        tmpAveragedNml += dstMesh.normals[dstVtxIdx];
                    }

                    tmpAveragedNml /= dstVtxPosIdxPair.Value.Count;
                }


                foreach (var dstVtxIdx in dstVtxPosIdxPair.Value)
                {
                    averagedNmlArray[dstVtxIdx] = tmpAveragedNml.normalized;
                }


                var dstVtxPos = dstVtxPosIdxPair.Key;

                Color      tmpVtxCol;
                Vector3    tmpVtxNml;
                BoneWeight tmpBoneWt;
                Vector3    intersectedPoint = Vector3.positiveInfinity;

                var srcMatchSignTriangleList = new List<TAP.TriangleData>();
                TAP.TriangleData srcIntersectedTriangle = null;
                
                var searchCount = 1;
                var searchedTriangleCount = 0;

                var firstVoxelAddress = dstBelongingSpaceOfVtxPosDict.FirstOrDefault(
                                            x => x.Value.Contains(dstVtxPos)).Key;

                var searchedVoxelAddressList = new List<Vector3>();
                var searchedTriangleIdxList  = new List<int>();

                switch (typeOfSearchMethod)
                {
                    case TypeOfSearchMethod.UseAveragedNormal: // 平均化した法線を元に検索
                        // srcIntersectedTriangle = TAP.GetIntersectedTriangleDataWithNormal(
                        //                             dstVtxPos,
                        //                             tmpAveragedNml,
                        //                             srcTriangleDataList,
                        //                             out srcMatchSignTriangleList);

                        
                        TAP.GetIntersectedTriangleDataWithAveragedNormal(
                            dstTrans,
                            dstVtxPos,
                            tmpAveragedNml,
                            firstVoxelAddress,
                            srcTriangleDataList,
                            srcVtxPosTriangleIdxDict,
                            srcBelongingSpaceOfVtxPosDict,
                            ref searchedVoxelAddressList,
                            ref searchedTriangleIdxList,
                            ref srcIntersectedTriangle,
                            ref intersectedPoint,
                            ref searchCount,
                            ref searchedTriangleCount);
                        break;

                    // case TypeOfSearchMethod.UseExistingNormal: // 既存の法線を元に検索
                    //     srcIntersectedTriangle = GetIntersectedTriangleDataWithNormal(
                    //                                 dstVtxPos,
                    //                                 dstVtxNml,
                    //                                 srcTriangleDataList,
                    //                                 out srcMatchSignTriangleList);
                    //     break;

                    case TypeOfSearchMethod.UseVertexPosition: // dstVtxPos の位置を元に検索
                        // Debug.Log("GetIntersectedTriangleDataWithVtxPos  =====================================");
                        // srcIntersectedTriangle = TAP.GetIntersectedTriangleDataWithVtxPos(
                        //                             dstVtxPos,
                        //                             srcTriangleDataList,
                        //                             srcVtxPosIdxDict,
                        //                             srcVtxPosTriangleIdxDict,
                        //                             ref intersectPoint,
                        //                             ref srcMatchSignTriangleList,
                        //                             ref searchCount);
                        // srcIntersectedTriangle = TAP.GetIntersectedTriangleDataWithVtxPos_Voxel (
                        //                             dstVtxPos,
                        //                             srcTriangleDataList,
                        //                             srcVtxPosIdxDict,
                        //                             srcVtxPosTriangleIdxDict,
                        //                             srcBelongingSpaceOfVtxPosDict,
                        //                             dstBelongingSpaceOfVtxPosDict,
                        //                             ref intersectedPoint,
                        //                             ref srcMatchSignTriangleList,
                        //                             ref searchCount,
                        //                             ref hitCount);

                        // var firstVoxelAddress = dstBelongingSpaceOfVtxPosDict.FirstOrDefault(
                        //                 x => x.Value.Contains(dstVtxPos)).Key;
                        // var searchedVoxelAddressList = new List<Vector3>();
                        
                        TAP.GetIntersectedTriangleDataWithVtxPos(
                            dstVtxPos,
                            firstVoxelAddress,
                            srcTriangleDataList,
                            // srcVtxPosIdxDict,
                            srcVtxPosTriangleIdxDict,
                            srcBelongingSpaceOfVtxPosDict,
                            // dstBelongingSpaceOfVtxPosDict,
                            ref searchedVoxelAddressList,
                            ref srcIntersectedTriangle,
                            ref intersectedPoint,
                            // ref outSrcMatchSignTriangleList,
                            ref searchCount);
                        break;
                }


                if (srcIntersectedTriangle != null)
                {
                    if (toggleMoveEachVtx)
                    {
                        // 法線を元に srcIntersectedTriangle 上の交差点を取得

                        // if (typeOfSearchMethod == TypeOfSearchMethod.UseAveragedNormal)
                        // {
                        //     intersectedPoint = TAP.GetIntersectedPointFromTriangleData(
                        //                         dstVtxPos,
                        //                         tmpAveragedNml,
                        //                         srcIntersectedTriangle);
                        // }
                        
                        var tmpIntersectWorldPos = Vector3.zero;
                        // var dis                  = 0.0;
                        // intersectPoint がVector3.positiveInfinityの場合はパス
                        if (TAP.IsVector3Nan(intersectedPoint) == false)
                        {
                            foreach (var dstVtxIdx in dstVtxPosIdxPair.Value)
                            {
                                // 交差点に頂点を移動させる
                                // (InverseTransformPointを使ってワールドスペースからローカルスペースに変換)
                                // if (dstTrans != null)
                                // {
                                //     vertices[dstVtxIdx]  = dstTrans.InverseTransformPoint(intersectedPoint);
                                //     tmpIntersectWorldPos = dstTrans.InverseTransformPoint(intersectedPoint);
                                // }
                                // else
                                // {
                                    vertices[dstVtxIdx]  = intersectedPoint;
                                    tmpIntersectWorldPos = intersectedPoint;
                                // }
                            }
                            // dis = Vector3.Distance(dstVtxPos, tmpIntersectWorldPos);
                        }
    // Debug.LogWarning("============  GetIntersectedTriangleDataWithVtxPos - srcIntersectedTriangleはありました");
    // Debug.LogWarning("vertex idx     = " + dstVtxIdx + "  :  searchCount = " + searchCount);
    // Debug.LogWarning("worldPos       = " + dstVtxPos.x + " , " + dstVtxPos.y + " , " + dstVtxPos.z);
    // // Debug.LogWarning("vtx nml        = " + dstMesh.normals[dstVtxIdx].x + " , " + dstMesh.normals[dstVtxIdx].y + " , " + dstMesh.normals[dstVtxIdx].z);
    // Debug.LogWarning("intersectPoint = " + tmpIntersectWorldPos.x + " , " + tmpIntersectWorldPos.y + " , " + tmpIntersectWorldPos.z);
                        
                        // if (dis > 0.2f && dstVtxPos.y < 0.15f)
                        // {
                        // output += "=================================================================\n";
                        // output += "vertex idx     = " + dstVtxIdx + "  :  searchCount = " + searchCount + "  :  hitCount = " + hitCount + "\n";
                        // output += "dstVtxPos      = " + dstVtxPos.x + " , " + dstVtxPos.y + " , " + dstVtxPos.z + "\n";
                        // output += "triangle index = " + srcIntersectedTriangle.triangleIdx + "\n";
                        // output += "intersectPoint = " + tmpIntersectWorldPos.x + " , " + tmpIntersectWorldPos.y + " , " + tmpIntersectWorldPos.z + "\n";
                        // output += "distance       = " + dis + "\n";
                        // output += "tmpAveragedNml = " + tmpAveragedNml.x + " , " + tmpAveragedNml.y + " , " + tmpAveragedNml.z + "\n";
                        // output += "\n";
                        // }
                    }

                    // srcIntersectedTriangle の中で dstVtxPos と一番近い頂点のカラー・法線・ボーンウェイトを取得する
                    TAP.GetAttributeFromClosetVtx(
                        dstVtxPos,
                        srcIntersectedTriangle,
                        out tmpVtxCol,
                        out tmpVtxNml,
                        out tmpBoneWt);

                    foreach (var dstVtxIdx in dstVtxPosIdxPair.Value)
                    {
                        if (toggleVtxColorTransfer == true)
                        {
                            vtxColors[dstVtxIdx] = tmpVtxCol;
                        }

                        if (toggleVtxNmlTransfer == true)
                        {
                            vtxNmls[dstVtxIdx] = tmpVtxNml;
                        }

                        if (toggleBoneWtTransfer == true)
                        {
                            boneWts[dstVtxIdx] = tmpBoneWt;
                        }
                    }
                }
                else // 交差する srcTriangleData が取得できなかった場合
                {
                    foreach (var dstVtxIdx in dstVtxPosIdxPair.Value)
                    {
                        // GetIntersectedTriangleDataで座標の符号が１つでもマッチするTriangleDataを取得できているか
                        if (srcMatchSignTriangleList != null)
                        {
                            // 現在回している dstVtxIdx と dstVtxPos からそれが所属する dstTriangle を取得
                            var dstTriangle = TAP.GetTriangleDataFromIdx(dstVtxIdx, dstVtxPos, dstTriangleDataList);
                
                            var minDistance = Mathf.Infinity;

                            TAP.TriangleData srcClosestTriangleData = null;

                            // dstTriangle と srcMatchSignTriangleList 内のTriangleDataとの centerPos の距離を比較して
                            // 一番近い srcClosestTriangleData を取得する
                            foreach (var srcMatchSignTriangle in srcMatchSignTriangleList)
                            {
                                var centerDistance = Vector3.Distance(
                                                        dstTriangle.centerPos,
                                                        srcMatchSignTriangle.centerPos);
                                if (centerDistance < minDistance)
                                {
                                    minDistance = centerDistance;
                                    srcClosestTriangleData = srcMatchSignTriangle;
                                }
                            }

                            if (srcClosestTriangleData != null)
                            {
                                // srcClosestTriangleDataの中でdstVtxPosと
                                // 一番近い頂点のカラー・法線・ボーンウェイトを取得する
                                TAP.GetAttributeFromClosetVtx(
                                    dstVtxPos,
                                    srcClosestTriangleData,
                                    out tmpVtxCol,
                                    out tmpVtxNml,
                                    out tmpBoneWt);

                                if (toggleVtxColorTransfer == true)
                                {
                                    vtxColors[dstVtxIdx] = tmpVtxCol;
                                }

                                if (toggleVtxNmlTransfer == true)
                                {
                                    vtxNmls[dstVtxIdx] = tmpVtxNml;
                                }

                                if (toggleBoneWtTransfer == true)
                                {
                                    boneWts[dstVtxIdx] = tmpBoneWt;
                                }
                            }
        Debug.LogWarning("=========================  StoreAveragedNormalsInTexcoord　 -   closestTriangleData が見つかりませんでした");
        // Debug.LogWarning("closestTriangleData = " + srcClosestTriangleData.triangleIdx);
        Debug.LogWarning("vertex idx     = " + dstVtxIdx );
        Debug.LogWarning("worldPos       = " + dstVtxPos.x + " , " + dstVtxPos.y + " , " + dstVtxPos.z);
        Debug.LogWarning("vtx nml        = " + dstMesh.normals[dstVtxIdx].x + " , " + dstMesh.normals[dstVtxIdx].y + " , " + dstMesh.normals[dstVtxIdx].z);
        Debug.LogWarning("tmpAveragedNml = " + tmpAveragedNml.x + " , " + tmpAveragedNml.y + " , " + tmpAveragedNml.z);
                        }
                    }

                    // output += "=================================================================\n";
                    // output += "vertex idx     = " + dstVtxIdx + "\n";
                    // output += "worldPos       = " + dstVtxPos.x + " , " + dstVtxPos.y + " , " + dstVtxPos.z + "\n";
                    // output += "tmpAveragedNml = " + tmpAveragedNml.x + " , " + tmpAveragedNml.y + " , " + tmpAveragedNml.z + "\n";
                    // output += "\n";
                    
                }
                // loopCount2++;
                // }
                loopCount++;
            }
            EditorUtility.ClearProgressBar();

            // TextSave(output, "TransferAttributesWindow_GetIntersectedTriangleDataWithVtxPos_Log");

            if (toggleStoreAveragedNml)
            {
                if (typeOfStoreNml == TypeOfStoreNml.Normal)
                {
                    dstMesh.normals = averagedNmlArray;
                }
                else if (typeOfStoreNml == TypeOfStoreNml.Tangent)
                {
                    // normalsはVector3の配列だが、SetTangentはVector4のリストが必要なので作る
                    var nmlForTangent    = new List<Vector4>();
                    var averageNmlLength = averagedNmlArray.Length;
                    foreach (var nml in averagedNmlArray)
                    {
                        nmlForTangent.Add(new Vector4(nml.x, nml.y, nml.z, 0));
                    }
                    dstMesh.SetTangents(nmlForTangent);
                }
                else
                {
                    var tmpNmlArray     = dstMesh.normals;
                    var quatList         = new List<Vector4>();
                    var averageNmlLength = averagedNmlArray.Length;

                    for (var i = 0; i < averageNmlLength; i++)
                    {
                        var quat = Quaternion.FromToRotation(averagedNmlArray[i], tmpNmlArray[i]);
                        quatList.Add(new Vector4(quat.x, quat.y, quat.z, quat.w));
                    }

                    switch (typeOfStoreNml)
                    {
                        case TypeOfStoreNml.UV_Channel_1:
                            dstMesh.SetUVs(0, averagedNmlArray.ToList());
                            break;
                    
                        case TypeOfStoreNml.UV_Channel_2:
                            dstMesh.SetUVs(1, averagedNmlArray.ToList());
                            break;
                    
                        case TypeOfStoreNml.UV_Channel_3:
                            dstMesh.SetUVs(2, averagedNmlArray.ToList());
                            break;
                    
                        case TypeOfStoreNml.UV_Channel_4:
                            dstMesh.SetUVs(3, averagedNmlArray.ToList());
                            break;
                    }
                }
            }

            if (toggleMoveEachVtx)
            {
                dstMesh.vertices = vertices;
            }

            if (toggleVtxColorTransfer)
            {
                dstMesh.colors = vtxColors;
            }

            if (toggleVtxNmlTransfer)
            {
                dstMesh.normals = vtxNmls;
            }

            if (toggleBoneWtTransfer)
            {
                dstMesh.boneWeights = boneWts;
            }
        }
        
        private static void TextSave(string txt, string fileName)
        {
            var sw = new System.IO.StreamWriter(
                "E:/works/Unity_Test/" + fileName + ".txt", false); //true=追記 false=上書き
            sw.WriteLine(txt);
            sw.Flush();
            sw.Close();
        }


        /// <summary>
        /// Apply texture color to vertex color or texcoord.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="tex"></param>
        private static void ApplyTextureColorToVertexColor(ref Mesh mesh, Texture2D tex)
        {
            var colorArrayFromTex = new Color[mesh.vertexCount];

            for (var i = 0; i < mesh.vertexCount; i++)
            {
                colorArrayFromTex[i] = tex.GetPixelBilinear(mesh.uv[i].x, mesh.uv[i].y);

                progress = (float)i / mesh.vertexCount;
                progressInfo = "Apply Texture Color To Vertex Color - " + mesh.name + " : " +
                                Mathf.Round(progress * 100).ToString() +
                                "% ( " + i.ToString() + " / " + mesh.vertexCount.ToString() + " )";

                isProgressCancel = EditorUtility.DisplayCancelableProgressBar(
                                    "Transfer Attributes",
                                    progressInfo,
                                    progress);
                // キャンセルされたら強制return
                if (isProgressCancel == true)
                {
                    Debug.LogWarning("ApplyTextureColorToVertexColor  :  キャンセルされました");
                    return;
                }
            }
            EditorUtility.ClearProgressBar();

            if (typeOfStoreVtxColor == TypeOfStoreVtxColor.Vertex_Color)
            {
                mesh.SetColors(colorArrayFromTex.ToList());
            }
            else
            {
                // UVにセットするためにColorをVector4に変換する
                var vec4ListFromColor = new List<Vector4>();
                foreach (var colorFromTex in colorArrayFromTex)
                {
                    vec4ListFromColor.Add(colorFromTex);
                }

                if (typeOfStoreVtxColor == TypeOfStoreVtxColor.UV_Channel_1)
                {
                    mesh.SetUVs(0, vec4ListFromColor);
                }
                else if (typeOfStoreVtxColor == TypeOfStoreVtxColor.UV_Channel_2)
                {
                    mesh.SetUVs(1, vec4ListFromColor);
                }
                else if (typeOfStoreVtxColor == TypeOfStoreVtxColor.UV_Channel_3)
                {
                    mesh.SetUVs(2, vec4ListFromColor);
                }
                else if (typeOfStoreVtxColor == TypeOfStoreVtxColor.UV_Channel_4)
                {
                    mesh.SetUVs(3, vec4ListFromColor);
                }
            }
        }


/*
第6章 EditorGUI (EdirotGUILayout) - エディター拡張入門
https://anchan828.github.io/editor-manual/web/part1-editorgui.html
*/
        private class BackgroundColorScope : GUI.Scope
        {
            private readonly Color color;
            public BackgroundColorScope(Color color)
            {
                this.color = GUI.backgroundColor;
                GUI.backgroundColor = color;
            }

            protected override void CloseScope()
            {
                GUI.backgroundColor = color;
            }
        }
    }
}