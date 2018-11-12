
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TransferAttributes
{
    using TAW = TransferAttributes.TransferAttributesWindow;
    public class TransferAttributesProcess
    {
        public class TriangleData
        {
            public int        triangleIdx;
            public int        vtxIdx1;
            public int        vtxIdx2;
            public int        vtxIdx3;
            public Vector3    vtxPos1;
            public Vector3    vtxPos2;
            public Vector3    vtxPos3;
            public Vector3    vtxNml1;
            public Vector3    vtxNml2;
            public Vector3    vtxNml3;
            public Color      vtxCol1;
            public Color      vtxCol2;
            public Color      vtxCol3;
            public BoneWeight boneWt1;
            public BoneWeight boneWt2;
            public BoneWeight boneWt3;
            public Vector3    edgeAB;
            public Vector3    edgeBC;
            public Vector3    edgeCA;
            public Vector3    centerPos;
            public Vector3    faceNml;
            public float      d; // 平面式で使うd. 点から平面までの距離

        }


        public static void GetTriangleDataList (
            Mesh targetMesh,
            MeshFilter mf,
            SkinnedMeshRenderer smr,
            bool hasVtxColorsInTargetMesh,
            bool hasBoneWtsInTargetMesh,
            out Dictionary<Vector3, List<int>> vtxPosIdxDict,
            out Dictionary<Vector3, List<int>> vtxPosTriangleIdxDict,
            out List<TriangleData> triangleDataList)
        {
            // Debug.Log("targetMesh = " + targetMesh.name + " : mf = " + mf + " : smr = " + smr);

            var progress         = 0.0f;
            var progressInfo     = "";
            var isProgressCancel = false;

            var targetTriangles = targetMesh.triangles;
            var triangleCount   = targetTriangles.Length / 3;
            var targetVertices  = targetMesh.vertices;
            var targetNormals   = targetMesh.normals;
            var targetColors    = targetMesh.colors;
            var targetBoneWts   = targetMesh.boneWeights;

            var tmpVtxPosIdxDict         = new Dictionary<Vector3, List<int>>();
            var tmpVtxPosTriangleIdxDict = new Dictionary<Vector3, List<int>>();
            var tmpTriangleDataList      = new List<TriangleData>();
            
            for (var i = 0; i < triangleCount; i++)
            {
                progress = (float)i / triangleCount;
                progressInfo = "Get Triangle Data From Mesh - " + targetMesh.name + " : " +
                                Mathf.Round(progress * 100).ToString() +
                                "% ( " + i.ToString() + " / " + triangleCount.ToString() + " )";

                isProgressCancel = EditorUtility.DisplayCancelableProgressBar(
                                    "Transfer Attributes",
                                    progressInfo,
                                    progress);
                // キャンセルされたら強制return
                if (isProgressCancel == true)
                {
                    Debug.LogWarning("GetTriangleDataList  :  キャンセルされました");
                
                    vtxPosIdxDict         = null;
                    vtxPosTriangleIdxDict = null;
                    triangleDataList      = null;
                    return;
                }

                var vtxIdx1 = targetTriangles[i * 3 + 0];
                var vtxIdx2 = targetTriangles[i * 3 + 1];
                var vtxIdx3 = targetTriangles[i * 3 + 2];

                var triangleVtxWorldPos1 = new Vector3(0, 0, 0);
                var triangleVtxWorldPos2 = new Vector3(0, 0, 0);
                var triangleVtxWorldPos3 = new Vector3(0, 0, 0);

                // TransformPointを使ってローカルスペースからワールドスペースの位置に変換
                if (mf != null)
                {
                    triangleVtxWorldPos1 = mf.transform.TransformPoint(targetVertices[vtxIdx1]);
                    triangleVtxWorldPos2 = mf.transform.TransformPoint(targetVertices[vtxIdx2]);
                    triangleVtxWorldPos3 = mf.transform.TransformPoint(targetVertices[vtxIdx3]);
                }
                else if (smr != null)
                {
                    triangleVtxWorldPos1 = smr.transform.TransformPoint(targetVertices[vtxIdx1]);
                    triangleVtxWorldPos2 = smr.transform.TransformPoint(targetVertices[vtxIdx2]);
                    triangleVtxWorldPos3 = smr.transform.TransformPoint(targetVertices[vtxIdx3]);
                }
                else
                {
                    triangleVtxWorldPos1 = targetVertices[vtxIdx1];
                    triangleVtxWorldPos2 = targetVertices[vtxIdx2];
                    triangleVtxWorldPos3 = targetVertices[vtxIdx3];
                }
                
                if (tmpVtxPosIdxDict.ContainsKey(triangleVtxWorldPos1) == false)
                {
                    tmpVtxPosIdxDict.Add(triangleVtxWorldPos1, new List<int>());
                }
                tmpVtxPosIdxDict[triangleVtxWorldPos1].Add(vtxIdx1);

                if (tmpVtxPosIdxDict.ContainsKey(triangleVtxWorldPos2) == false)
                {
                    tmpVtxPosIdxDict.Add(triangleVtxWorldPos2, new List<int>());
                }
                tmpVtxPosIdxDict[triangleVtxWorldPos2].Add(vtxIdx2);

                if (tmpVtxPosIdxDict.ContainsKey(triangleVtxWorldPos3) == false)
                {
                    tmpVtxPosIdxDict.Add(triangleVtxWorldPos3, new List<int>());
                }
                tmpVtxPosIdxDict[triangleVtxWorldPos3].Add(vtxIdx3);


                var triangleEdgeAB = triangleVtxWorldPos2 - triangleVtxWorldPos1;
                var triangleEdgeBC = triangleVtxWorldPos3 - triangleVtxWorldPos2;
                var triangleEdgeCA = triangleVtxWorldPos1 - triangleVtxWorldPos3;

                var vtxCenterPosX = (triangleVtxWorldPos1.x + triangleVtxWorldPos2.x + triangleVtxWorldPos3.x) / 3;
                var vtxCenterPosY = (triangleVtxWorldPos1.y + triangleVtxWorldPos2.y + triangleVtxWorldPos3.y) / 3;
                var vtxCenterPosZ = (triangleVtxWorldPos1.z + triangleVtxWorldPos2.z + triangleVtxWorldPos3.z) / 3;

                var triangleCenterPos = new Vector3(vtxCenterPosX, vtxCenterPosY, vtxCenterPosZ);


                if (tmpVtxPosTriangleIdxDict.ContainsKey(triangleVtxWorldPos1) == false)
                {
                    tmpVtxPosTriangleIdxDict.Add(triangleVtxWorldPos1, new List<int>());
                }
                tmpVtxPosTriangleIdxDict[triangleVtxWorldPos1].Add(i);

                if (tmpVtxPosTriangleIdxDict.ContainsKey(triangleVtxWorldPos2) == false)
                {
                    tmpVtxPosTriangleIdxDict.Add(triangleVtxWorldPos2, new List<int>());
                }
                tmpVtxPosTriangleIdxDict[triangleVtxWorldPos2].Add(i);

                if (tmpVtxPosTriangleIdxDict.ContainsKey(triangleVtxWorldPos3) == false)
                {
                    tmpVtxPosTriangleIdxDict.Add(triangleVtxWorldPos3, new List<int>());
                }
                tmpVtxPosTriangleIdxDict[triangleVtxWorldPos3].Add(i);

                if (tmpVtxPosTriangleIdxDict.ContainsKey(triangleCenterPos) == false)
                {
                    tmpVtxPosTriangleIdxDict.Add(triangleCenterPos, new List<int>());
                }
                tmpVtxPosTriangleIdxDict[triangleCenterPos].Add(i);

                
                var triangleFaceNormal = Vector3.Cross(triangleEdgeAB, triangleEdgeBC).normalized;
                
                var triangleD = -1 * ((triangleFaceNormal.x * triangleCenterPos.x) + 
                                      (triangleFaceNormal.y * triangleCenterPos.y) + 
                                      (triangleFaceNormal.z * triangleCenterPos.z));
                
                var tmpTriangleData         = new TriangleData();
                tmpTriangleData.triangleIdx = i;
                tmpTriangleData.vtxIdx1     = vtxIdx1;
                tmpTriangleData.vtxIdx2     = vtxIdx2;
                tmpTriangleData.vtxIdx3     = vtxIdx3;
                tmpTriangleData.vtxPos1     = triangleVtxWorldPos1;
                tmpTriangleData.vtxPos2     = triangleVtxWorldPos2;
                tmpTriangleData.vtxPos3     = triangleVtxWorldPos3;
                tmpTriangleData.vtxNml1     = targetNormals[vtxIdx1];
                tmpTriangleData.vtxNml2     = targetNormals[vtxIdx2];
                tmpTriangleData.vtxNml3     = targetNormals[vtxIdx3];
                tmpTriangleData.edgeAB      = triangleEdgeAB;
                tmpTriangleData.edgeBC      = triangleEdgeBC;
                tmpTriangleData.edgeCA      = triangleEdgeCA;
                tmpTriangleData.centerPos   = triangleCenterPos;
                tmpTriangleData.faceNml     = triangleFaceNormal;
                tmpTriangleData.d           = triangleD;

                if (hasVtxColorsInTargetMesh == true)
                {
                    tmpTriangleData.vtxCol1 = targetColors[vtxIdx1];
                    tmpTriangleData.vtxCol2 = targetColors[vtxIdx2];
                    tmpTriangleData.vtxCol3 = targetColors[vtxIdx3];
                }

                if (smr != null && hasBoneWtsInTargetMesh == true)
                {
                    tmpTriangleData.boneWt1 = targetBoneWts[vtxIdx1];
                    tmpTriangleData.boneWt2 = targetBoneWts[vtxIdx2];
                    tmpTriangleData.boneWt3 = targetBoneWts[vtxIdx3];
                }
                
                tmpTriangleDataList.Add(tmpTriangleData);

                // output += "=================================================================\n";
                // output += "triangleCount idx     = " + i + "\n";
                // output += "triangleCenterPoint   = " + triangleCenterPoint.x + " , " + triangleCenterPoint.y + " , " + triangleCenterPoint.z + "\n";
                // output += "triangleFaceNormal    = " + triangleFaceNormal.x + " , " + triangleFaceNormal.y + " , " + triangleFaceNormal.z + "\n";
                // output += "triangle D            = " + triangleD + "\n";
                // output += "\n";
            }
            EditorUtility.ClearProgressBar();
                        
            // TextSave(output, "TransferAttributesWindow_TriangleData_Log");

            vtxPosIdxDict      = tmpVtxPosIdxDict;
            // Debug.Log("vtxPosIdxDict Count = " + vtxPosIdxDict.Count);

            vtxPosTriangleIdxDict = tmpVtxPosTriangleIdxDict;
            // Debug.Log("vtxPosTriangleIdxDict Count = " + vtxPosTriangleIdxDict.Count);

            triangleDataList   = tmpTriangleDataList;
        }


        /// <summary>
        /// Get closest triangle data of intersected ray.
        /// </summary>
        /// <param name="dstVtxIdx"></param>
        /// <param name="dstVtxPos"></param>
        /// <param name="dstVtxNml"></param>
        /// <param name="dstTriangleDataList"></param>
        /// <param name="srcTriangleDataList"></param>
        /// <returns></returns>
        public static TriangleData GetIntersectedTriangleDataWithNormal (
            Vector3 dstVtxPos,
            Vector3 dstVtxNml,
            List<TriangleData> srcTriangleDataList,
            out List<TriangleData> srcMatchSignTriangleList)
        {
            srcMatchSignTriangleList = null;

            TriangleData intersectedTriangleData = null;
            var minDistance = Mathf.Infinity;

            var isPosX_PlusSign = dstVtxPos.x > 0;
            var isPosY_PlusSign = dstVtxPos.y > 0;
            var isPosZ_PlusSign = dstVtxPos.z > 0;

            // dstVtxPos の符号と srcTriangleData の符号が一つでもマッチしているTriangleDataを入れるリスト
            var tmpSrcMatchSignTriangleList = new List<TriangleData>();

            var progress         = 0.0f;
            var progressInfo     = "";
            var isProgressCancel = false;

            var loopCount = 0;
            var dictCount = srcTriangleDataList.Count;
            
            foreach (var srcTriangleData in srcTriangleDataList)
            {
                progress = (float)loopCount / dictCount;
                progressInfo = "Get Intersected Triangle Data With Normal : " +
                                Mathf.Round(progress * 100).ToString() +
                                "% ( " + loopCount.ToString() + " / " + dictCount.ToString() + " )";

                isProgressCancel = EditorUtility.DisplayCancelableProgressBar(
                                    "Transfer Attributes",
                                    progressInfo,
                                    progress);
                // キャンセルされたら強制return
                if (isProgressCancel == true)
                {
                    Debug.LogWarning("GetIntersectedTriangleDataWithNormal  :  キャンセルされました");
                    return null;
                }

                // まず srcTriangleData の中心点の符号をチェック
                var isCenterX_PlusSign = srcTriangleData.centerPos.x > 0;
                var isCenterY_PlusSign = srcTriangleData.centerPos.y > 0;
                var isCenterZ_PlusSign = srcTriangleData.centerPos.z > 0;

                // dstVtxPos の符号と srcTriangleData の中心点の符号が全部違う場合はパス
                if (isPosX_PlusSign != isCenterX_PlusSign
                &&  isPosY_PlusSign != isCenterY_PlusSign
                &&  isPosZ_PlusSign != isCenterZ_PlusSign)
                {
                    continue;
                }

                tmpSrcMatchSignTriangleList.Add(srcTriangleData);

                // 面の法線と dstVtxNml のRayの内積の絶対値がMathf.Epsilon以下
                // （つまりこの2つのベクトルがなす角度がほぼ90度）の場合はパス
                var dotFaceRay = Vector3.Dot(srcTriangleData.faceNml, dstVtxNml);
                if (Mathf.Abs(dotFaceRay) <= Mathf.Epsilon)
                {
                    continue;
                }

                if (HasPointOnTriangle(srcTriangleData, dstVtxPos, dstVtxNml))
                {
                    var distanceToTriangle = (dstVtxPos - srcTriangleData.centerPos).sqrMagnitude;
                    if (distanceToTriangle < minDistance)
                    {
                        minDistance             = distanceToTriangle;
                        intersectedTriangleData = srcTriangleData;
                    }
                }
                loopCount++;
            }
            EditorUtility.ClearProgressBar();

            if (intersectedTriangleData == null)
            {
                srcMatchSignTriangleList = tmpSrcMatchSignTriangleList;
            }
            else
            {
                srcMatchSignTriangleList = null;
            }

            return intersectedTriangleData;
        }


        /// <summary>
        /// Get intersected triangle data with averaged normal.
        /// </summary>
        /// <param name="dstTrans"></param>
        /// <param name="dstVtxPos"></param>
        /// <param name="dstAveragedNml"></param>
        /// <param name="firstVoxelAddress"></param>
        /// <param name="srcTriangleDataList"></param>
        /// <param name="srcVtxPosTriangleIdxDict"></param>
        /// <param name="srcBelongingSpaceOfVtxPosDict"></param>
        /// <param name="searchedVoxelAddressList"></param>
        /// <param name="searchedTriangleIdxList"></param>
        /// <param name="intersectedTriangleData"></param>
        /// <param name="intersectedPoint"></param>
        /// <param name="searchCount"></param>
        /// <param name="searchedTriangleCount"></param>
        /// <returns></returns>
        public static TriangleData GetIntersectedTriangleDataWithAveragedNormal (
            Transform dstTrans,
            Vector3 dstVtxPos,
            Vector3 dstAveragedNml,
            Vector3 firstVoxelAddress,
            List<TriangleData> srcTriangleDataList,
            Dictionary<Vector3, List<int>> srcVtxPosTriangleIdxDict,
            Dictionary<Vector3, List<Vector3>> srcBelongingSpaceOfVtxPosDict,
            ref List<Vector3> searchedVoxelAddressList,
            ref List<int> searchedTriangleIdxList,
            ref TriangleData intersectedTriangleData,
            ref Vector3 intersectedPoint,
            ref int searchCount,
            ref int searchedTriangleCount)
        {
            if (searchCount > 10) { return null; }

            var tmpIntersectedPoint = Vector3.positiveInfinity;
            
            var allVoxelAddressCount = srcBelongingSpaceOfVtxPosDict.Count;

            
            var isTriangeGet = false;
            var isSearchedVoxelAddressFinish = false;
            TriangleData tmpIntersectedTriangleData = null;

            var minVtxDistance = Mathf.Infinity;


            for (var x = searchCount * -1; x <= searchCount; x++)
            {
                if (isSearchedVoxelAddressFinish == true) { break; }

                for (var y = searchCount * -1; y <= searchCount; y++)
                {
                    if (isSearchedVoxelAddressFinish == true) { break; }

                    for (var z = searchCount * -1; z <= searchCount; z++)
                    {
                        if (isSearchedVoxelAddressFinish == true) { break; }

                        var tmpVoxelAddress = new Vector3(
                                                firstVoxelAddress.x + x,
                                                firstVoxelAddress.y + y,
                                                firstVoxelAddress.z + z);

                        // すでに検索済みのVoxelAddressならパス
                        if (searchedVoxelAddressList.Contains(tmpVoxelAddress) == true) { continue; }

                        // tmpVoxelAddress が存在するアドレスかどうかチェック
                        if (srcBelongingSpaceOfVtxPosDict.ContainsKey(tmpVoxelAddress) == false) { continue; }

                        var srcVtxPosListInSameVoxel = srcBelongingSpaceOfVtxPosDict[tmpVoxelAddress];
                        
                        // アドレスが存在していてもその中に頂点のリストがなかったら意味がないのでパス
                        if (srcVtxPosListInSameVoxel.Count == 0) { continue; }


                        TriangleData getIntersectedTriangleData = null;
                        var getIntersectedPoint = Vector3.positiveInfinity;
                        GetIntersectedTriangleDataWithVoxelAndAveragedNormal(
                            dstTrans,
                            dstVtxPos,
                            dstAveragedNml,
                            tmpVoxelAddress,
                            srcTriangleDataList,
                            srcVtxPosTriangleIdxDict,
                            srcVtxPosListInSameVoxel,
                            searchCount,
                            ref getIntersectedTriangleData,
                            ref getIntersectedPoint,
                            ref searchedTriangleIdxList,
                            allVoxelAddressCount,
                            searchedVoxelAddressList.Count,
                            ref searchedTriangleCount);
                        
                        if (getIntersectedTriangleData != null)
                        // &&  getIntersectedPoint != Vector3.positiveInfinity)
                        {
                            var distanceDstVtxPos = (dstVtxPos - getIntersectedPoint).sqrMagnitude;
                            if (distanceDstVtxPos < minVtxDistance)
                            {
                                minVtxDistance             = distanceDstVtxPos;
                                tmpIntersectedTriangleData = getIntersectedTriangleData;
                                tmpIntersectedPoint        = getIntersectedPoint;
                                isTriangeGet               = true;
                            }
                        }
                        
                        // 検索済みのVoxelAddressをリストに追加
                        searchedVoxelAddressList.Add(tmpVoxelAddress);

                        if (allVoxelAddressCount <= searchedVoxelAddressList.Count)
                        {
                            Debug.LogWarning("voxelAddressCount と searchedVoxelAddressList.Count の" +
                                "数が一緒になってしまいました・・・");

                            isSearchedVoxelAddressFinish = true;
                        }
                    }
                }
            }
            

            if (isTriangeGet == true)
            {
                intersectedTriangleData = tmpIntersectedTriangleData;
                intersectedPoint        = tmpIntersectedPoint;
            }

            if (isSearchedVoxelAddressFinish == true)
            {
                Debug.LogWarning("isSearchedVoxelAddressFinish : 全部のボクセルを検索し終わってしまった…");
                return null;
            }

            
            if (isTriangeGet == false)
            {
                searchCount++;

                GetIntersectedTriangleDataWithAveragedNormal(
                    dstTrans,
                    dstVtxPos,
                    dstAveragedNml,
                    firstVoxelAddress,
                    srcTriangleDataList,
                    srcVtxPosTriangleIdxDict,
                    srcBelongingSpaceOfVtxPosDict,
                    ref searchedVoxelAddressList,
                    ref searchedTriangleIdxList,
                    ref tmpIntersectedTriangleData,
                    ref tmpIntersectedPoint,
                    ref searchCount,
                    ref searchedTriangleCount);

                if (tmpIntersectedTriangleData != null
                &&  tmpIntersectedPoint != Vector3.positiveInfinity)
                {
                    intersectedTriangleData = tmpIntersectedTriangleData;
                    intersectedPoint        = tmpIntersectedPoint;
                }
                
            }

            return intersectedTriangleData;
        }


        /// <summary>
        /// Get intersected triangle data with averaged normal.
        /// </summary>
        /// <param name="dstVtxPos"></param>
        /// <param name="dstAveragedNml"></param>
        /// <param name="voxelAddress"></param>
        /// <param name="srcTriangleDataList"></param>
        /// <param name="Dictionary<Vector3"></param>
        /// <param name="srcVtxPosTriangleIdxDict"></param>
        /// <param name="srcVtxPosListInSameVoxel"></param>
        /// <param name="searchCount"></param>
        /// <param name="intersectedTriangleData"></param>
        /// <param name="intersectedPoint"></param>
        /// <param name="allVoxelListCount"></param>
        /// <param name="curVoxelListCount"></param>
        public static void GetIntersectedTriangleDataWithVoxelAndAveragedNormal (
            Transform dstTrans,
            Vector3 dstVtxPos,
            Vector3 dstAveragedNml,
            Vector3 voxelAddress,
            List<TriangleData> srcTriangleDataList,
            Dictionary<Vector3, List<int>> srcVtxPosTriangleIdxDict,
            List<Vector3> srcVtxPosListInSameVoxel,
            int searchCount,
            ref TriangleData intersectedTriangleData,
            ref Vector3 intersectedPoint,
            ref List<int> searchedTriangleIdxList,
            int allVoxelListCount,
            int curVoxelListCount,
            ref int searchedTriangleCount)
        {
            var minVtxDistance = Mathf.Infinity;

            var srcTriangleIdxListInSameVoxel = new List<int>();
            foreach (var srcVtxPosInSameVoxel in srcVtxPosListInSameVoxel)
            {
                var tmpTriangleIdxList = srcVtxPosTriangleIdxDict[srcVtxPosInSameVoxel];
                foreach (var tmpTriangleIdx in tmpTriangleIdxList)
                {
                    if (srcTriangleIdxListInSameVoxel.Contains(tmpTriangleIdx) == false)
                    {
                        srcTriangleIdxListInSameVoxel.Add(tmpTriangleIdx);
                    }
                }
            }

            var tmpIntersectedPoint = Vector3.positiveInfinity;

            var isPosX_PlusSign = dstVtxPos.x > 0;
            var isPosY_PlusSign = dstVtxPos.y > 0;
            var isPosZ_PlusSign = dstVtxPos.z > 0;

            // dstVtxPos の符号と srcTriangleData の符号が一つでもマッチしているTriangleDataを入れるリスト
            var tmpSrcMatchSignTriangleList = new List<TriangleData>();


            foreach (var srcTriangleIdxInSameVoxel in srcTriangleIdxListInSameVoxel)
            {
                searchedTriangleCount++;

                // すでに検索しているTriangleIdxだったらパス
                if (searchedTriangleIdxList.Contains(srcTriangleIdxInSameVoxel) == true)
                {
                    continue;
                }
                else
                {
                    searchedTriangleIdxList.Add(srcTriangleIdxInSameVoxel);
                }

                var tmpSrcTriangle = srcTriangleDataList[srcTriangleIdxInSameVoxel];


                // まず srcTriangleData の中心点の符号をチェック
                var isCenterX_PlusSign = tmpSrcTriangle.centerPos.x > 0;
                var isCenterY_PlusSign = tmpSrcTriangle.centerPos.y > 0;
                var isCenterZ_PlusSign = tmpSrcTriangle.centerPos.z > 0;

                // dstVtxPos の符号と srcTriangleData の中心点の符号が全部違う場合はパス
                if (isPosX_PlusSign != isCenterX_PlusSign
                &&  isPosY_PlusSign != isCenterY_PlusSign
                &&  isPosZ_PlusSign != isCenterZ_PlusSign)
                {
                    continue;
                }

                tmpSrcMatchSignTriangleList.Add(tmpSrcTriangle);

                // 面の法線と dstVtxNml のRayの内積から、この2つのベクトルがなす角度が90以上の場合はパス
                var dotFaceRay = Vector3.Dot(tmpSrcTriangle.faceNml, dstAveragedNml);
                if (Mathf.Abs(dotFaceRay) <= Mathf.Epsilon)
                {
                    continue;
                }

                tmpIntersectedPoint = GetIntersectedPointFromTriangleData(
                                        dstVtxPos,
                                        dstAveragedNml,
                                        tmpSrcTriangle);


                if (HasPointOnTriangle(tmpSrcTriangle, tmpIntersectedPoint, dstAveragedNml))
                {
                    var distanceToTriangle = (dstVtxPos - tmpIntersectedPoint).sqrMagnitude;

                    if (distanceToTriangle < minVtxDistance)
                    {
                        minVtxDistance          = distanceToTriangle;
                        intersectedTriangleData = tmpSrcTriangle;
                        intersectedPoint        = tmpIntersectedPoint;
                    }
                }
            }

            // EditorUtility.ClearProgressBar();
            
            if (intersectedTriangleData == null)
            {
                intersectedTriangleData = null;
                intersectedPoint        = Vector3.positiveInfinity;
                return;
            }
        }



        /// <summary>
        /// Check nearest triangle with voxel.
        /// </summary>
        /// <param name="dstVtxPos"></param>
        /// <param name="firstVoxelAddress"></param>
        /// <param name="srcTriangleDataList"></param>
        /// <param name="srcVtxPosTriangleIdxDict"></param>
        /// <param name="srcBelongingSpaceOfVtxPosDict"></param>
        /// <param name="searchedVoxelAddressList"></param>
        /// <param name="intersectedTriangleData"></param>
        /// <param name="intersectedPoint"></param>
        /// <param name="searchCount"></param>
        public static void GetIntersectedTriangleDataWithVtxPos (
            Vector3 dstVtxPos,
            Vector3 firstVoxelAddress,
            List<TriangleData> srcTriangleDataList,
            Dictionary<Vector3, List<int>> srcVtxPosTriangleIdxDict,
            Dictionary<Vector3, List<Vector3>> srcBelongingSpaceOfVtxPosDict,
            ref List<Vector3> searchedVoxelAddressList,
            ref TriangleData intersectedTriangleData,
            ref Vector3 intersectedPoint,
            ref int searchCount)
        {
            var tmpIntersectedPoint = Vector3.positiveInfinity;

            var voxelAddressCount = srcBelongingSpaceOfVtxPosDict.Count;

            
            var isTriangeGet = false;
            var isSearchedVoxelAddressFinish = false;
            TriangleData tmpIntersectedTriangleData = null;

            var minVtxDistance = Mathf.Infinity;

            for (var x = searchCount * -1; x <= searchCount; x++)
            {
                for (var y = searchCount * -1; y <= searchCount; y++)
                {
                    for (var z = searchCount * -1; z <= searchCount; z++)
                    {
                        if (isSearchedVoxelAddressFinish == true) { break; }

                        var tmpVoxelAddress = new Vector3(
                                                firstVoxelAddress.x + x,
                                                firstVoxelAddress.y + y,
                                                firstVoxelAddress.z + z);

                        // すでに検索済みのVoxelAddressならパス
                        if (searchedVoxelAddressList.Contains(tmpVoxelAddress) == true) { continue; }

                        // tmpVoxelAddress が存在するアドレスかどうかチェック
                        if (srcBelongingSpaceOfVtxPosDict.ContainsKey(tmpVoxelAddress) == false) { continue; }

                        var srcVtxPosListInSameVoxel = srcBelongingSpaceOfVtxPosDict[tmpVoxelAddress];

                        // アドレスが存在していてもその中に頂点のリストがなかったら意味がないのでパス
                        if (srcVtxPosListInSameVoxel.Count == 0) { continue; }
                        
                        TriangleData getIntersectedTriangleData = null;
                        var getIntersectedPoint = Vector3.positiveInfinity;
                        GetIntersectedTriangleDataWithVoxelAndVtxPos(
                            dstVtxPos,
                            tmpVoxelAddress,
                            srcTriangleDataList,
                            srcVtxPosTriangleIdxDict,
                            srcVtxPosListInSameVoxel,
                            searchCount,
                            ref getIntersectedTriangleData,
                            ref getIntersectedPoint);
                        
                        if (getIntersectedTriangleData != null
                        &&  getIntersectedPoint != Vector3.positiveInfinity)
                        {
                            var distanceDstVtxPos = (dstVtxPos - getIntersectedPoint).sqrMagnitude;
                            if (distanceDstVtxPos < minVtxDistance)
                            {
                                minVtxDistance             = distanceDstVtxPos;
                                tmpIntersectedTriangleData = getIntersectedTriangleData;
                                tmpIntersectedPoint        = getIntersectedPoint;
                                isTriangeGet = true;
                            }
                        }
                        
                        // 検索済みのVoxelAddressをリストに追加
                        searchedVoxelAddressList.Add(tmpVoxelAddress);

                        if (voxelAddressCount <= searchedVoxelAddressList.Count)
                        {
                            Debug.LogWarning("voxelAddressCount と searchedVoxelAddressList.Count の" +
                                "数が一緒になってしまいました・・・");
                            isSearchedVoxelAddressFinish = true;
                        }
                    }
                }
            }

            if (isTriangeGet == true)
            {
                intersectedTriangleData = tmpIntersectedTriangleData;
                intersectedPoint        = tmpIntersectedPoint;
            }       
            
            if (isTriangeGet == false
            ||  intersectedTriangleData == null
            ||  intersectedPoint == Vector3.positiveInfinity)
            {
                searchCount++;

                GetIntersectedTriangleDataWithVtxPos(
                    dstVtxPos,
                    firstVoxelAddress,
                    srcTriangleDataList,
                    srcVtxPosTriangleIdxDict,
                    srcBelongingSpaceOfVtxPosDict,
                    ref searchedVoxelAddressList,
                    ref tmpIntersectedTriangleData,
                    ref tmpIntersectedPoint,
                    ref searchCount);

                if (tmpIntersectedTriangleData != null
                &&  tmpIntersectedPoint != Vector3.positiveInfinity)
                {
                    intersectedTriangleData = tmpIntersectedTriangleData;
                    intersectedPoint        = tmpIntersectedPoint;
                }
                
            }
        }


        /// <summary>
        /// Get intersected triangle data with voxel.
        /// </summary>
        /// <param name="dstVtxPos"></param>
        /// <param name="voxelAddress"></param>
        /// <param name="srcTriangleDataList"></param>
        /// <param name="srcVtxPosTriangleIdxDict"></param>
        /// <param name="srcVtxPosListInSameVoxel"></param>
        /// <param name="searchCount"></param>
        /// <param name="intersectedTriangleData"></param>
        /// <param name="intersectedPoint"></param>
        public static void GetIntersectedTriangleDataWithVoxelAndVtxPos (
            Vector3 dstVtxPos,
            Vector3 voxelAddress,
            List<TriangleData> srcTriangleDataList,
            Dictionary<Vector3, List<int>> srcVtxPosTriangleIdxDict,
            List<Vector3> srcVtxPosListInSameVoxel,
            int searchCount,
            ref TriangleData intersectedTriangleData,
            ref Vector3 intersectedPoint)
        {
            var minVtxDistance = Mathf.Infinity;

            var loopCount = 0;
            var listCount = srcVtxPosListInSameVoxel.Count;

            var nearestSrcVtxPos = Vector3.positiveInfinity;
            foreach (var srcVtxPosInSameVoxel in srcVtxPosListInSameVoxel)
            {
                var distanceDstVtxPos = (dstVtxPos - srcVtxPosInSameVoxel).sqrMagnitude;

                if (distanceDstVtxPos < minVtxDistance)
                {
                    nearestSrcVtxPos = srcVtxPosInSameVoxel;
                    minVtxDistance   = distanceDstVtxPos;
                }
                loopCount++;
            }


            if (srcVtxPosTriangleIdxDict.ContainsKey(nearestSrcVtxPos) == false)
            {
                intersectedTriangleData = null;
                intersectedPoint        = Vector3.positiveInfinity;
            }

            var nearestSrcTriangleIdxList = srcVtxPosTriangleIdxDict[nearestSrcVtxPos];
            var minCenterVtxDistance      = Mathf.Infinity;

            TriangleData nearestSrcTriangle = null;
            foreach (var nearestTriangleIdx in nearestSrcTriangleIdxList)
            {
                var tmpNearestTriangle = srcTriangleDataList[nearestTriangleIdx];

                var vtxDistance_C = (dstVtxPos - tmpNearestTriangle.centerPos).magnitude;

                if (vtxDistance_C < minCenterVtxDistance)
                {
                    nearestSrcTriangle   = tmpNearestTriangle;
                    minCenterVtxDistance = vtxDistance_C;
                }
            }


            if (nearestSrcTriangle != null)
            {
                var perpendicularLine   = (nearestSrcTriangle.faceNml * -1).normalized;
                var outIntersectedPoint = Vector3.positiveInfinity;

                if (HitCheckTriangleToSphere(
                    nearestSrcTriangle,
                    nearestSrcVtxPos,
                    dstVtxPos,
                    perpendicularLine,
                    ref outIntersectedPoint))
                {
                    intersectedTriangleData = nearestSrcTriangle;
                    intersectedPoint        = outIntersectedPoint;
                }
                else
                {
                    intersectedTriangleData = null;
                    intersectedPoint        = Vector3.positiveInfinity;
                }
            }
            else
            {
                intersectedTriangleData = null;
                intersectedPoint        = Vector3.positiveInfinity;
            }

        }


        /// <summary>
        /// Hit check triangle to sphere.
        /// </summary>
        /// <param name="srcTriangleData"></param>
        /// <param name="nearestSrcVtxPos"></param>
        /// <param name="dstVtxPos"></param>
        /// <param name="perpendicularLine"></param>
        /// <param name="intersectedPoint"></param>
        /// <returns></returns>
        public static bool HitCheckTriangleToSphere (
            TriangleData srcTriangleData,
            Vector3 nearestSrcVtxPos,
            Vector3 dstVtxPos,
            Vector3 perpendicularLine,
            ref Vector3 intersectedPoint)
        {
            intersectedPoint = GetIntersectedPointFromTriangleData(
                                dstVtxPos,
                                perpendicularLine,
                                srcTriangleData);
            
            // 上記で得た intersectPoint が srcTriangleData の中に入っているのかをチェックしていく
            if (HasPointOnTriangle(srcTriangleData, intersectedPoint, perpendicularLine))
            {
                return true;
            }


            // intersectPoint が srcTriangleData の中に入っていなかったので、
            // dstVtxPos から srcTriangleData の各エッジ上の最近点を求める
            var cPos  = srcTriangleData.centerPos;
            var vPos1 = srcTriangleData.vtxPos1;
            var vPos2 = srcTriangleData.vtxPos2;
            var vPos3 = srcTriangleData.vtxPos3;

            var tmpIntersectPoint1 = Vector3.positiveInfinity;
            var tmpIntersectPoint2 = Vector3.positiveInfinity;
            var tmpIntersectPoint3 = Vector3.positiveInfinity;
            var dist               = Mathf.Infinity;
            
            if (nearestSrcVtxPos == vPos1 || nearestSrcVtxPos == vPos2)
            {
                tmpIntersectPoint1 = GetIntersectedPointOnEdge(dstVtxPos, vPos1, vPos2);
                
                if (tmpIntersectPoint1 != Vector3.positiveInfinity)
                {
                    var distance_1 = (cPos - tmpIntersectPoint1).sqrMagnitude;
                    if (distance_1 < dist)
                    {
                        intersectedPoint = tmpIntersectPoint1;
                        dist             = distance_1;
                    }
                }
            }

            if (nearestSrcVtxPos == vPos2 || nearestSrcVtxPos == vPos3)
            {
                tmpIntersectPoint2 = GetIntersectedPointOnEdge(dstVtxPos, vPos2, vPos3);
                
                if (tmpIntersectPoint2 != Vector3.positiveInfinity)
                {
                    var distance_2 = (cPos - tmpIntersectPoint2).sqrMagnitude;
                    if (distance_2 < dist)
                    {
                        intersectedPoint = tmpIntersectPoint2;
                        dist             = distance_2;
                    }
                }
            }

            if (nearestSrcVtxPos == vPos3 || nearestSrcVtxPos == vPos1)
            {
                tmpIntersectPoint3 = GetIntersectedPointOnEdge(dstVtxPos, vPos3, vPos1);

                if (tmpIntersectPoint3 != Vector3.positiveInfinity)
                {
                    var distance_3 = (cPos - tmpIntersectPoint3).sqrMagnitude;
                    if (distance_3 < dist)
                    {
                        intersectedPoint = tmpIntersectPoint3;
                        dist             = distance_3;
                    }
                }
            }

            if (dist != Mathf.Infinity)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Get intersected point on the edge.
        /// </summary>
        /// <param name="intersectedPoint"></param>
        /// <param name="edge_start"></param>
        /// <param name="edge_end"></param>
        /// <returns></returns>
        public static Vector3 GetIntersectedPointOnEdge (
            Vector3 intersectedPoint,
            Vector3 edge_start,
            Vector3 edge_end)
        {
            var afterIntersecedtPoint = Vector3.positiveInfinity;
/*
2.2 点と直線との距離
http://www.infra.kochi-tech.ac.jp/takagi/Survey2/7Parameter.pdf

点A(Xa, Ya, Za)を通り、ベクトルAB(ABx, ABy, ABz)で向きが表されている空間直線と
点P(Xp, Yp, Zp)との最短距離 T を求める。

点Pからその直線上の点QへのベクトルPQ(PQx, PQy, PQz)を表すと
PQx = Xa + (ABx * T) - Xp
PQy = Ya + (ABy * T) - Yp
PQz = Za + (ABz * T) - Zp

このベクトルPQとベクトルABは直行するので、内積は0となるため、それを元に T を求める。
(ABx * PQx) + (ABy * PQy) + (ABz * PQz) = 0
(ABx * (Xa + (ABx * T) - Xp))) + (ABy * (Ya + (ABy * T) - Yp))) + (ABz * (Za + (ABz * T) - Zp))) = 0
((ABx * ABx) + (ABy * ABy) + (ABz * ABz)) * T = (ABx * (Xp - Xa)) + (ABy * (Yp - Ya)) + (ABz * (Zp - Za))
T = (ABx * (Xp - Xa)) + (ABy * (Yp - Ya)) + (ABz * (Zp - Za)) / ((ABx * ABx) + (ABy * ABy) + (ABz * ABz))

この除算の分子はベクトルABとベクトルAPとの内積で求められるので、
T = Vector3.Dot(AB , AP) / ((ABx * ABx) + (ABy * ABy) + (ABz * ABz))

T を用いて点Qの座標を表すと
Xq = Xa + (ABx * T)
Yq = Ya + (ABy * T)
Zq = Za + (ABz * T)
*/
            // edge_start を点A、edge_end を点B、intersectPoint を点Pとして計算していく
            var edgeAB = (edge_end - edge_start);
            var edgeAP = (intersectedPoint - edge_start);
            
            var dot = Vector3.Dot(edgeAB, edgeAP);

            var t = dot / ((edgeAB.x * edgeAB.x) + (edgeAB.y * edgeAB.y) + (edgeAB.z * edgeAB.z));

            afterIntersecedtPoint = edge_start + (edgeAB * t);

            Vector3 fixedIntersectedPoint;
            if (HasPointOnSegment(afterIntersecedtPoint, edge_start, edge_end, out fixedIntersectedPoint) == false)
            {
                afterIntersecedtPoint = fixedIntersectedPoint;
            }
            
            return afterIntersecedtPoint;
        }


        /// <summary>
        /// Has this point on the segment ?
        /// </summary>
        /// <param name="point"></param>
        /// <param name="edge_start"></param>
        /// <param name="edge_end"></param>
        /// <param name="fixedPoint"></param>
        /// <returns></returns>
        public static bool HasPointOnSegment (
            Vector3 point,
            Vector3 edge_start,
            Vector3 edge_end,
            out Vector3 fixedPoint)
        {
            fixedPoint = Vector3.positiveInfinity;

            var dist_AB = Vector3.Distance(edge_start, edge_end);
            var dist_AP = Vector3.Distance(edge_start, point);
            var dist_BP = Vector3.Distance(edge_end, point);

            // もし AP + BP が AB よりも長い場合、AB の中に P が収まっていない
            if (dist_AB < dist_AP + dist_BP)
            {
                // P と近いのが A か B かをチェックし、近い方に置き換える
                var dist_min = Mathf.Min(dist_AP, dist_BP);
                if (dist_min == dist_AP)
                {
                    fixedPoint = edge_start;
                }
                else
                {
                    fixedPoint = edge_end;
                }
                return false;
            }

            return true;
        }


        /// <summary>
        /// Has this dstIntersectedPoint on srcTriangleData ?
        /// </summary>
        /// <param name="srcTriangleData"></param>
        /// <param name="dstIntersectedPoint"></param>
        /// <param name="dstNormal"></param>
        /// <returns></returns>
        public static bool HasPointOnTriangle (
            TriangleData srcTriangleData,
            Vector3 dstIntersectedPoint,
            Vector3 dstNormal)
        {
            var srcVtxPos1 = srcTriangleData.vtxPos1;
            var srcVtxPos2 = srcTriangleData.vtxPos2;
            var srcVtxPos3 = srcTriangleData.vtxPos3;

            // 頂点へのエッジ
            var srcEdgePA = srcVtxPos1 - dstIntersectedPoint;
            var srcEdgePB = srcVtxPos2 - dstIntersectedPoint;
            var srcEdgePC = srcVtxPos3 - dstIntersectedPoint;

            // 各法線
            var srcNmlABP = Vector3.Cross(srcEdgePA, srcTriangleData.edgeAB).normalized;
            var srcNmlBCP = Vector3.Cross(srcEdgePB, srcTriangleData.edgeBC).normalized;
            var srcNmlCAP = Vector3.Cross(srcEdgePC, srcTriangleData.edgeCA).normalized;

            // 内積を求める
            var dot1 = Vector3.Dot(srcNmlABP, dstNormal);
            var dot2 = Vector3.Dot(srcNmlBCP, dstNormal);
            var dot3 = Vector3.Dot(srcNmlCAP, dstNormal);

            // 3つの内積の符号が同じなら、その三角形とrayは交差している
            if (( (dot1 >= 0) && (dot2 >= 0) && (dot3 >= 0) )
            ||  ( (dot1 <= 0) && (dot2 <= 0) && (dot3 <= 0) ))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        
        /// <summary>
        /// Get attributes( VertexColor / VertexNormal / BoneWeight) from closet vertex in srcClosestTriangleData.
        /// </summary>
        /// <param name="dstVtxPos"></param>
        /// <param name="srcClosestTriangleData"></param>
        /// <param name="vtxColor"></param>
        /// <param name="vtxNormal"></param>
        /// <param name="boneWt"></param>
        public static void GetAttributeFromClosetVtx (
            Vector3 dstVtxPos,
            TriangleData srcClosestTriangleData,
            out Color vtxColor,
            out Vector3 vtxNormal,
            out BoneWeight boneWt)
        {
            // Rayと交差した最近接のTriangleDataからどの頂点が一番近いかの調べる
            var dist1_1    = Vector3.Distance(dstVtxPos, srcClosestTriangleData.vtxPos1);
            var dist1_2    = Vector3.Distance(dstVtxPos, srcClosestTriangleData.vtxPos2);
            var dist1_3    = Vector3.Distance(dstVtxPos, srcClosestTriangleData.vtxPos3);
            var dist1Array = new float[]{dist1_1, dist1_2, dist1_3};
            var dist1_min  = dist1Array.Min();

            // 一番近かった頂点の情報を取得し、outで渡す
            if (dist1_min == dist1_1) // 一番距離が近いのが vtxPos1 の場合
            {
                vtxNormal = srcClosestTriangleData.vtxNml1;
                vtxColor  = srcClosestTriangleData.vtxCol1;
                boneWt    = srcClosestTriangleData.boneWt1;
            }
            else if (dist1_min == dist1_2) // 一番距離が近いのが vtxPos2 の場合
            {
                vtxNormal = srcClosestTriangleData.vtxNml2;
                vtxColor  = srcClosestTriangleData.vtxCol2;
                boneWt    = srcClosestTriangleData.boneWt2;
            }
            else // (dist1_min == dist1_3) 一番距離が近いのが vtxPos3 の場合
            {   
                vtxNormal = srcClosestTriangleData.vtxNml3;
                vtxColor  = srcClosestTriangleData.vtxCol3;
                boneWt    = srcClosestTriangleData.boneWt3;
            }
        }


        /// <summary>
        /// Get triangle data from vertex index and vertex pos.
        /// </summary>
        /// <param name="vtxIdx"></param>
        /// <param name="vtxPos"></param>
        /// <param name="triangleDataList"></param>
        /// <returns></returns>
        public static TriangleData GetTriangleDataFromIdx (
            int vtxIdx,
            Vector3 vtxPos,
            List<TriangleData> triangleDataList)
        {
            TriangleData matchTriangleData = null;

            foreach (var triangleData in triangleDataList)
            {
                if (triangleData.vtxIdx1 == vtxIdx
                ||  triangleData.vtxIdx2 == vtxIdx
                ||  triangleData.vtxIdx3 == vtxIdx)
                {
                    if (triangleData.vtxPos1 == vtxPos
                    ||  triangleData.vtxPos2 == vtxPos
                    ||  triangleData.vtxPos3 == vtxPos)
                    {
                        matchTriangleData = triangleData;
                        break;
                    }
                }
            }

            return matchTriangleData;
        }


        /// <summary>
        /// Get intersected point in the argument triangleData.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="ray"></param>
        /// <param name="triangleData"></param>
        /// <returns></returns>
        public static Vector3 GetIntersectedPointFromTriangleData (
            Vector3 pos,
            Vector3 ray,
            TriangleData triangleData)
        {
            var a = triangleData.faceNml.x;
            var b = triangleData.faceNml.y;
            var c = triangleData.faceNml.z;

            // var x = triangleData.vtxPos1.x;
            // var y = triangleData.vtxPos1.y;
            // var z = triangleData.vtxPos1.z;

            // // 平面式の ax + by + cz + d = 0　を元に定数 d を求めるために d = (ax + by + cz) * -1 として計算
            var d = triangleData.d; // ((a * x) + (b * y) + (c * z)) * -1;

/*
交点 P を求めるためには pos + (ray * t) という方程式のスカラー値 t を求めればよい
それをもとにPの座標はこのように置き換えることができる
P.x = pos.x + (ray.x * t)
P.y = pos.y + (ray.y * t)
P.z = pos.z + (ray.z * t)

このPを通る平面は上記で求めた平面と同じであるため、dは同じものを使用し、平面式にPの座標を当てはめると
a(pos.x + (ray.x * t)) + b(pos.y + (ray.y * t)) + c(pos.z + (ray.z * t)) + d = 0

これを展開し整理していく
(a * pos.x) + (a * ray.x * t) + (b * pos.y) + (b * ray.y * t) + (c * pos.z) + (c * ray.z * t) + d = 0

t を掛けている部分をまとめて、それ以外の部分を右辺に移動（符号が変わるので*-1でまとめる）
t((a * ray.x) + (b * ray.y) + (c * ray.z)) = ((a * pos.x) + (b * pos.y) + (c * pos.z) + d) * -1

最後に t を求めるために両辺を(a * ray.x) + (b * ray.y) + (c * ray.z)で割る
*/
            var t = (((a * pos.x) + (b * pos.y) + (c * pos.z) + d) * -1) /
                    ((a * ray.x) + (b * ray.y) + (c * ray.z));

            return pos + (ray * t);
        }

        

        /// <summary>
        /// Change read/write enable in texture.
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="texImporter"></param>
        /// <param name="isReadable"></param>
        public static void ChangeReadWriteEnableInTexture (
            Texture2D tex,
            TextureImporter texImporter,
            bool isReadable)
        {
            // Debug.Log("texImporter.isReadable = " + texImporter.isReadable + " : isReadable = " + isReadable);
            if (texImporter.isReadable != isReadable)
            {
                texImporter.isReadable = isReadable;
                texImporter.SaveAndReimport();
            }
        }


        /// <summary>
        /// If this Vector3 is NaN or Infinity, return true.
        /// </summary>
        /// <param name="vec3"></param>
        /// <returns></returns>
        public static bool IsVector3Nan (Vector3 vec3)
        {
            if (float.IsNaN(vec3.x) || float.IsNaN(vec3.x) || float.IsNaN(vec3.x))
            {
                return true;
            }
            else if (float.IsInfinity(vec3.x) || float.IsInfinity(vec3.x) || float.IsInfinity(vec3.x))
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Register the belonging space of vtx pos in the meshes.
        /// </summary>
        /// <param name="srcMeshBoundsMax"></param>
        /// <param name="srcMeshBoundsMin"></param>
        /// <param name="srcTriangleDataList"></param>
        /// <param name="Dictionary<Vector3"></param>
        /// <param name="srcBelongingSpaceOfVtxPosDict"></param>
        /// <param name="dstMeshBoundsMax"></param>
        /// <param name="dstMeshBoundsMin"></param>
        /// <param name="dstTriangleDataList"></param>
        /// <param name="Dictionary<Vector3"></param>
        /// <param name="dstBelongingSpaceOfVtxPosDict"></param>
        /// <param name="voxelSpaceLength"></param>
        /// <param name="Dictionary<Vector3"></param>
        /// <param name="voxelSpaceDict"></param>
        public static void RegisterBelongingSpaceOfVtxPosInMeshes (
            Vector3 srcMeshBoundsMax,
            Vector3 srcMeshBoundsMin,
            List<TriangleData> srcTriangleDataList,
            ref Dictionary<Vector3, List<Vector3>> srcBelongingSpaceOfVtxPosDict,
            Vector3 dstMeshBoundsMax,
            Vector3 dstMeshBoundsMin,
            List<TriangleData> dstTriangleDataList,
            ref Dictionary<Vector3, List<Vector3>> dstBelongingSpaceOfVtxPosDict,
            ref float voxelSpaceLength,
            ref Dictionary<Vector3, Bounds> voxelSpaceDict)
        {
            // srcMeshBounds と dstMeshBounds のXYZそれぞれを比較して、両方を含めたBoundsのMax、Minを取得
            Vector3 boundsMax;
            boundsMax.x = srcMeshBoundsMax.x > dstMeshBoundsMax.x ? srcMeshBoundsMax.x : dstMeshBoundsMax.x;
            boundsMax.y = srcMeshBoundsMax.y > dstMeshBoundsMax.y ? srcMeshBoundsMax.y : dstMeshBoundsMax.y;
            boundsMax.z = srcMeshBoundsMax.z > dstMeshBoundsMax.z ? srcMeshBoundsMax.z : dstMeshBoundsMax.z;

            Vector3 boundsMin;
            boundsMin.x = srcMeshBoundsMin.x < dstMeshBoundsMin.x ? srcMeshBoundsMin.x : dstMeshBoundsMin.x;
            boundsMin.y = srcMeshBoundsMin.y < dstMeshBoundsMin.y ? srcMeshBoundsMin.y : dstMeshBoundsMin.y;
            boundsMin.z = srcMeshBoundsMin.z < dstMeshBoundsMin.z ? srcMeshBoundsMin.z : dstMeshBoundsMin.z;

            var distanceX = boundsMax.x - boundsMin.x;
            var distanceY = boundsMax.y - boundsMin.y;
            var distanceZ = boundsMax.z - boundsMin.z;

            // boundsMaxの中で一番距離の長い（大きい）軸を求めて、それを10分割したものを検索するボクセルのサイズとする
            voxelSpaceLength = new float[]{distanceX, distanceY, distanceZ}.Max() / 10;

            // 各軸のdistanceを voxelSpaceLength で割り、CeilToIntで切り上げした整数に変換して
            // voxelSpaceLength が何個分か計算する
            var xVoxelCount = Mathf.CeilToInt(distanceX / voxelSpaceLength);
            var yVoxelCount = Mathf.CeilToInt(distanceY / voxelSpaceLength);
            var zVoxelCount = Mathf.CeilToInt(distanceZ / voxelSpaceLength);

            voxelSpaceDict = new Dictionary<Vector3, Bounds>();

            var progress         = 0.0f;
            var progressInfo     = "";
            var isProgressCancel = false;
            

            for (var x = 0; x < xVoxelCount; x++)
            {
                var tmpBoundsMaxX = boundsMax.x - (voxelSpaceLength * x);
                var tmpBoundsMinX = boundsMax.x - (voxelSpaceLength * (x + 1));
                // boundsMin.x よりも tmpBoundsMinX が小さくなっていたら boundsMin.x に置き換える
                if (tmpBoundsMinX < boundsMin.x)
                {
                    tmpBoundsMinX = boundsMin.x;
                }
                
                for (var y = 0; y < yVoxelCount; y++)
                {
                    var tmpBoundsMaxY = boundsMax.y - (voxelSpaceLength * y);
                    var tmpBoundsMinY = boundsMax.y - (voxelSpaceLength * (y + 1));

                    // boundsMin.y よりも tmpBoundsMinY が小さくなっていたら boundsMin.y に置き換える
                    if (tmpBoundsMinY < boundsMin.y)
                    {
                        tmpBoundsMinY = boundsMin.y;
                    }

                    for (var z = 0; z < zVoxelCount; z++)
                    {
                        var tmpBoundsMaxZ = boundsMax.z - (voxelSpaceLength * z);
                        var tmpBoundsMinZ = boundsMax.z - (voxelSpaceLength * (z + 1));
                        // boundsMin.z よりも tmpBoundsMinY が小さくなっていたら boundsMin.z に置き換える
                        if (tmpBoundsMinZ < boundsMin.z)
                        {
                            tmpBoundsMinZ = boundsMin.z;
                        }

                        progress = (float)(x + 1) / xVoxelCount;
                        progressInfo = "Register Belonging Space Of Vtx Pos In Meshes : " +
                                        Mathf.Round(progress * 100).ToString() +
                                        "% ( " + (x + 1).ToString() + " / " + xVoxelCount.ToString() + " )";

                        isProgressCancel = EditorUtility.DisplayCancelableProgressBar(
                                            "Transfer Attributes",
                                            progressInfo,
                                            progress);
                        // キャンセルされたら強制return
                        if (isProgressCancel == true)
                        {
                            Debug.LogWarning("Register Belonging Space Of Vtx Pos In Meshes : キャンセルされました");
                            EditorUtility.ClearProgressBar();
                            return;
                        }

                        var tmpBounds = new Bounds();
                        var min = new Vector3(tmpBoundsMinX, tmpBoundsMinY, tmpBoundsMinZ);
                        var max = new Vector3(tmpBoundsMaxX, tmpBoundsMaxY, tmpBoundsMaxZ);
                        tmpBounds.SetMinMax(min, max);

                        var tmpVoxelAddress = new Vector3(x, y, z);

                        voxelSpaceDict[tmpVoxelAddress] = tmpBounds;

                    }
                }
            }
            

            srcBelongingSpaceOfVtxPosDict = new Dictionary<Vector3, List<Vector3>>();

            foreach (var srcTriangleData in srcTriangleDataList)
            {
                var srcVtxPos1 = srcTriangleData.vtxPos1;
                var srcVtxPos2 = srcTriangleData.vtxPos2;
                var srcVtxPos3 = srcTriangleData.vtxPos3;

                foreach (var voxelSpaceAddress in voxelSpaceDict.Keys)
                {
                    var voxelBound = voxelSpaceDict[voxelSpaceAddress];

                    if (srcBelongingSpaceOfVtxPosDict.ContainsKey(voxelSpaceAddress) == false)
                    {
                        srcBelongingSpaceOfVtxPosDict[voxelSpaceAddress] = new List<Vector3>();
                    }

                    if (voxelBound.Contains(srcVtxPos1))
                    {
                        srcBelongingSpaceOfVtxPosDict[voxelSpaceAddress].Add(srcVtxPos1);
                    }

                    if (voxelBound.Contains(srcVtxPos2))
                    {
                        srcBelongingSpaceOfVtxPosDict[voxelSpaceAddress].Add(srcVtxPos2);
                    }

                    if (voxelBound.Contains(srcVtxPos3))
                    {
                        srcBelongingSpaceOfVtxPosDict[voxelSpaceAddress].Add(srcVtxPos3);
                    }
                }
            }


            dstBelongingSpaceOfVtxPosDict = new Dictionary<Vector3, List<Vector3>>();

            foreach (var dstTriangleData in dstTriangleDataList)
            {
                var dstVtxPos1 = dstTriangleData.vtxPos1;
                var dstVtxPos2 = dstTriangleData.vtxPos2;
                var dstVtxPos3 = dstTriangleData.vtxPos3;

                foreach (var voxelSpaceAddress in voxelSpaceDict.Keys)
                {
                    var voxelBound = voxelSpaceDict[voxelSpaceAddress];

                    if (dstBelongingSpaceOfVtxPosDict.ContainsKey(voxelSpaceAddress) == false)
                    {
                        dstBelongingSpaceOfVtxPosDict.Add(voxelSpaceAddress, new List<Vector3>());
                    }

                    if (voxelBound.Contains(dstVtxPos1))
                    {
                        dstBelongingSpaceOfVtxPosDict[voxelSpaceAddress].Add(dstVtxPos1);
                    }

                    if (voxelBound.Contains(dstVtxPos2))
                    {
                        dstBelongingSpaceOfVtxPosDict[voxelSpaceAddress].Add(dstVtxPos2);
                    }

                    if (voxelBound.Contains(dstVtxPos3))
                    {
                        dstBelongingSpaceOfVtxPosDict[voxelSpaceAddress].Add(dstVtxPos3);
                    }
                }
            }

            EditorUtility.ClearProgressBar();
        }


        public class PixelData
        {
            public int uvX;
            public int uvY;
            public Color blendCol;
        }
        private static Color[] vtxColorArray;
        public static Texture2D BakeVtxColToTexture (
            Mesh dstMesh,
            List<TriangleData> dstTriangleDataList,
            int texHeight,
            int texWidth,
            TextureFormat texFormat,
            bool useMipmap,
            TAW.TypeOfUseUV typeOfUseUV)
        {
            var vtxColTex = new Texture2D(texWidth, texHeight, texFormat, useMipmap);
            
            var pixelCount = texHeight * texWidth;
            // Debug.Log("pixelCount = " + pixelCount + "  :  texHeight = " + texHeight + "  :  texWidth = " + texWidth);
            
            // BlackTexture
            var blackCol = new Color(0, 0, 0, 1);
            // var blackColArray = Enumerable.Repeat(blackCol, pixelCount).ToArray();
            vtxColorArray = Enumerable.Repeat(blackCol, pixelCount).ToArray();

            // vtxColTex.SetPixels(blackColArray);
            // vtxColTex.SetPixels(textureBuffer);
            // vtxColTex.Apply(true, false);

            var count        = 0;
            var totalCount   = dstTriangleDataList.Count;
            var progress     = 0.0f;
            var progressInfo = "";

            // List<PixelData> pixelDataList;
            var applidColorPixelList = new List<int>();

            var uvList = new List<Vector2>();
            dstMesh.GetUVs((int)typeOfUseUV, uvList);

            foreach (var dstTriangleData in dstTriangleDataList)
            {
                count++;

                progress     = (float)count / totalCount;
                progressInfo = "TriangleIdx = " + dstTriangleData.triangleIdx +
                                " : " + count.ToString() + " / " + totalCount.ToString();

                EditorUtility.DisplayProgressBar(
                    "BakeVtxColToTexture",
                    progressInfo,
                    progress);

                CalcUVForTexture(
                    dstMesh,
                    dstTriangleData,
                    vtxColTex,
                    texHeight,
                    texWidth,
                    uvList,
                    // ref pixelDataList);
                    ref vtxColorArray,
                    ref applidColorPixelList,
                    progressInfo);

                // Debug.Log("pixelDataList = " + pixelDataList.Count);
                // foreach (var pixelData in pixelDataList)
                // {
                //     Debug.Log("pixelData = " + pixelData.uvX + " , " + pixelData.uvY +
                //         "  :  Color = " + pixelData.blendCol.r + ", " + pixelData.blendCol.g + ", " + pixelData.blendCol.b + ", " + pixelData.blendCol.a);
                //     vtxColTex.SetPixel(pixelData.uvX, pixelData.uvY, pixelData.blendCol);
                    
                // }
            }
            vtxColTex.SetPixels(vtxColorArray);

            EditorUtility.ClearProgressBar();

            vtxColTex.Apply(true, false);

            return vtxColTex;
        }


        public static void CalcUVForTexture (
            Mesh dstMesh,
            TriangleData dstTriangleData,
            Texture2D vtxColTex,
            int texHeight,
            int texWidth,
            List<Vector2> uvList,
            // ref List<PixelData> pixelDataList)
            ref Color[] vtxColArray,
            ref List<int> applidColorPixelList,
            string progressInfo)
        {
            var idx1 = dstTriangleData.vtxIdx1;
            var idx2 = dstTriangleData.vtxIdx2;
            var idx3 = dstTriangleData.vtxIdx3;

            var col1 = dstTriangleData.vtxCol1;
            var col2 = dstTriangleData.vtxCol2;
            var col3 = dstTriangleData.vtxCol3;

            var uv1 = uvList[idx1];
            var uv2 = uvList[idx2];
            var uv3 = uvList[idx3];

            var uvArrayX = new float[] { uv1.x, uv2.x, uv3.x };
            var uvArrayY = new float[] { uv1.y, uv2.y, uv3.y };

            var uvMaxX = uvArrayX.Max();
            var uvMinX = uvArrayX.Min();
            var uvMaxY = uvArrayY.Max();
            var uvMinY = uvArrayY.Min();

            var count            = 0;
            var totalCount       = texHeight * texWidth;
            var progress         = 0.0f;
            var progressInfo2    = "";
            var isProgressCancel = false;

            // UVと同じくテクスチャのチェックしている位置を0～1で表す変数
            Vector2 checkerPoint;
            for (var y = 0; y < texHeight; y++)
            {
                checkerPoint.y = (float)y / texHeight;
                // Debug.Log("checkerPoint.y = " + checkerPoint.y + " : uvMinY = " + uvMinY + " : uvMaxY = " + uvMaxY);
                
                // Yの最大よりも大きかったり、最小よりも小さい場合はパス
                if (uvMaxY < checkerPoint.y || checkerPoint.y < uvMinY) //{ continue; }
                {
                    count++;
                    continue;
                }

                for (var x = 0; x < texWidth; x++)
                {
                    count++;

                    progress     = (float)count / totalCount;
                    progressInfo2 = progressInfo + " : x = " + x + " : y = " + y +
                                    " : " + count.ToString() + " / " + totalCount.ToString();

                    isProgressCancel = EditorUtility.DisplayCancelableProgressBar(
                                        "Calculate UV For Texture",
                                        progressInfo2,
                                        progress);

                    // キャンセルされたら強制return
                    if (isProgressCancel == true)
                    {
                        Debug.LogWarning("CalcUVForTexture  :  キャンセルされました");
                        return;
                    }

                    checkerPoint.x = (float)x / texWidth;
                    // Debug.Log("checkerPoint.x = " + checkerPoint.x + " : uvMinX = " + uvMinX + " : uvMaxX = " + uvMaxX);
                    
                    // Xの最大よりも大きかったり、最小よりも小さい場合はパス
                    if (uvMaxX < checkerPoint.x || checkerPoint.x < uvMinX) { continue; }

                    // ここでUVによる三角形の各エッジと走査中の点（ピクセル）との内積を計算
                    if (HasPointOnTriangle2D(uv1, uv2, uv3, checkerPoint) == true)
                    {
                        // var tmpPixelData      = new PixelData();
                        // tmpPixelData.uvX      = (int)(checkerPoint.x * texWidth);
                        // tmpPixelData.uvY      = (int)(checkerPoint.y * texHeight);
                        
                        // Debug.Log("col1 = " + col1.r + ", " + col1.g + ", " + col1.b + ", " + col1.a +
                        //     "  :  col2 = " + col2.r + ", " + col2.g + ", " + col2.b + ", " + col2.a +
                        //     "  :  col3 = " + col3.r + ", " + col3.g + ", " + col3.b + ", " + col3.a );
                        // tmpPixelData.blendCol = GetBlendedColor(uv1, uv2, uv3, checkerPoint, col1, col2, col3);
                        // pixelDataList.Add(tmpPixelData);
                        var pixelIdx = x + texWidth * y;
                        
                        if (applidColorPixelList.Contains(pixelIdx) == false)
                        {
                            var blendCol = GetBlendedColor(uv1, uv2, uv3, checkerPoint, col1, col2, col3);
                            vtxColArray.SetValue(blendCol, pixelIdx);

                            Debug.Log("pixelIdx = " + pixelIdx +
                                "  :  blendCol = " + blendCol.r + ", " + blendCol.g + ", " + blendCol.b + ", " + blendCol.a);

                            applidColorPixelList.Add(pixelIdx);
                        }
                        
                    }
                }
            }
            EditorUtility.ClearProgressBar();

        }

        public static bool HasPointOnTriangle2D (
            Vector2 uv1,
            Vector2 uv2,
            Vector2 uv3,
            Vector2 checkerPoint)
        {
            // 渡された三角形のエッジ
            var edgeAB = uv2 - uv1;
            var edgeBC = uv3 - uv2;
            var edgeCA = uv1 - uv3;

            // checkerPoint と三角形の各点とのエッジ
            var edgePA = uv1 - checkerPoint;
            var edgePB = uv2 - checkerPoint;
            var edgePC = uv3 - checkerPoint;

            // 各エッジの外積
            var cross1 = Vector2Cross(edgePA, edgeAB);
            var cross2 = Vector2Cross(edgePB, edgeBC);
            var cross3 = Vector2Cross(edgePC, edgeCA);
            // Debug.Log("cross1 = " + cross1 + " : cross2 = " + cross2 + " : cross3 = " + cross3);

            // 3つの外積の符号が同じなら、checkerPoint は三角形の内側にある
            if (( (cross1 >= 0) && (cross2 >= 0) && (cross3 >= 0) )
            ||  ( (cross1 <= 0) && (cross2 <= 0) && (cross3 <= 0) ))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static float Vector2Cross (Vector2 lhs, Vector2 rhs)
        {
            return (lhs.x * rhs.y) - (rhs.x * lhs.y);
        }


/*
Interpolating in a Triangle - Code Plea
https://codeplea.com/triangular-interpolation

checkerPoint を P 、三角形ABCの3点からの距離のウェイトを W1 W2 W3 とした場合

Px = (Ax * W1) + (Bx * W2) + (Cx * W3);
Py = (Ay * W1) + (By * W2) + (Cy * W3);
W1 + W2 + W3 = 1;

上記のような式でPを求められる。
ウェイト W1 W2 W3 を求めるためにクラメルの公式を用いて式を解く。
三元連立一次方程式として解くので、

W1 + W2 + W3 = 1;

を Pz = 1 として置き換える。

Pz = (1 * W1) + (1 * W2) + (1 * W3);

これの係数行列 A は

    | Ax Bx Cx |
A = | Ay By Cy |
    |  1  1  1 |

となる。（ A != 0 でないといけない ）

この A を分母として

     | Px Bx Cx |
W1 = | Py By Cy | / A
     |  1  1  1 |

     | Ax Px Cx |
W2 = | Ay Py Cy | / A
     |  1  1  1 |

     | Ax Bx Px |
W3 = | Ay By Py | / A
     |  1  1  1 |

とすることができる。
これを展開して、

W1 = ( (Px * By * 1) + (1 * Bx * Cy) + (Ay * 1 * Cx) - (1 * By * Cx) - (Py * Bx * 1) - (Px * 1 * Cy) )
    / ( (Ax * By * 1) + (1 * Bx * Cy) + (Ay * 1 * Cx) - (1 * By * Cx) - (Ay * Bx * 1) - (Ax * 1 * Cy) )

W2 = ( (Ax * Py * 1) + (1 * Px * Cy) + (Ay * 1 * Cx) - (1 * Py * Cx) - (Ay * Px * 1) - (Ax * 1 * Cy) )
    / ( (Ax * By * 1) + (1 * Bx * Cy) + (Ay * 1 * Cx) - (1 * By * Cx) - (Ay * Bx * 1) - (Ax * 1 * Cy) )

W2 = ( (Ax * By * 1) + (1 * Bx * Py) + (Ay * 1 * Px) - (1 * By * Px) - (Ay * Bx * 1) - (Ax * 1 * Py) )
    / ( (Ax * By * 1) + (1 * Bx * Cy) + (Ay * 1 * Cx) - (1 * By * Cx) - (Ay * Bx * 1) - (Ax * 1 * Cy) )

*/
        public static Color GetBlendedColor (
            Vector2 uv1,
            Vector2 uv2,
            Vector2 uv3,
            Vector2 checkerPoint,
            Color col1,
            Color col2,
            Color col3)
        {
            var blendCol = new Color();

            var coef    = (uv1.x * uv2.y) + (uv2.x * uv3.y) + (uv1.y * uv3.x) -
                            (uv2.y * uv3.x) - (uv1.y * uv2.x) - (uv1.x * uv3.y);

            var weight1 = (checkerPoint.x * uv2.y) + (uv2.x * uv3.y) + (checkerPoint.y * uv3.x) -
                            (uv2.y * uv3.x) - (checkerPoint.y * uv2.x) - (checkerPoint.x * uv3.y);
            weight1 /= coef;
            
            var weight2 = (uv1.x * checkerPoint.y) + (checkerPoint.x * uv3.y) + (uv1.y * uv3.x) -
                            (checkerPoint.y * uv3.x) - (uv1.y * checkerPoint.x) - (uv1.x * uv3.y);
            weight2 /= coef;

            var weight3 = (uv1.x * uv2.y) + (uv2.x * checkerPoint.y) + (uv1.y * checkerPoint.x) -
                            (uv2.y * checkerPoint.x) - (uv1.y * uv2.x) - (uv1.x * checkerPoint.y);
            weight3 /= coef;

            
            // var weight1 = ((uv2.y - uv3.y) * (checkerPoint.x - uv3.x)) +
            //               ((uv3.x - uv2.x) * (checkerPoint.y - uv3.y));
                          
            // weight1 = weight1 / ((uv2.y - uv3.y) * (uv1.x - uv3.x)) + ((uv3.x - uv2.x) * (uv1.y - uv3.y));
            blendCol = (col1 * weight1) + (col2 * weight2) + (col3 * weight3);

            return blendCol;
        }
    }
}