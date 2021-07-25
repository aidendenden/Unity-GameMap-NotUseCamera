using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;


public class Map : MonoBehaviour
{
    public bool enableMap = true;

    public EMapMode MapMod;

    public GameMapData mapInfo = new GameMapData();

    public string playerID;

    private void Start()
    {
        MapDataInitialization(mapInfo);

        ClearMapIcon(mapInfo);

        InvokeRepeating(nameof(RefreshMap), 0, 0.5f);

        mapInfo.GameStartMod = MapMod;

        MapSwitch(mapInfo.GameStartMod);
    }

    private void Update()
    {
        if (enableMap)
        {
            mapInfo.mapCanvasRect.gameObject.SetActive(true);
            if (GetPlayerTransform() != null)
            {
                MapUpdate(GetPlayerTransform());
            }
            else
            {
                return;
            }
        }
        else
        {
            mapInfo.mapCanvasRect.gameObject.SetActive(false);
        }


        if (mapInfo.GameStartMod != MapMod)
        {
            mapInfo.switchMapType = true;
        }
    }


    #region init

    /// <summary>
    /// 拿玩家位置
    /// </summary>
    /// <param name="mapData"></param>
    /// <param name="prefab"></param>
    /// <returns></returns>
    public Transform GetPlayerTransform()
    {
        if (playerID == String.Empty)
        {
            return null;
            
            // if (GameLevel.Current.Actors.LocalPlayerController == null)
            // {
            //     Debug.LogWarning("Missing Local Player Control");
            //     return null;
            // }
            //
            // return GameLevel.Current.Actors.LocalPlayerController.Character.transform;
        }
        else
        {
            Debug.LogWarning("Missing Gamelevel or Actors");
            return null;
        }
    }


    /// <summary>
    /// Coordinate initialization
    /// 坐标初始化
    /// </summary>
    /// <param name="mapData"></param>
    /// <param name="prefab"></param>
    /// <returns></returns>
    public void MapDataInitialization(GameMapData mapData)
    {
        if (mapData.sceneMax != null && mapData.sceneMin != null)
        {
            mapData.sceneMaxV3 = mapData.sceneMax.position;
            mapData.sceneMinV3 = mapData.sceneMin.position;
        }

        if (mapData.sceneMaxV3 != null && mapData.sceneMinV3 != null)
        {
            mapData.sceneSize.y = mapData.sceneMaxV3.z - mapData.sceneMinV3.z;
            mapData.sceneSize.x = mapData.sceneMaxV3.x - mapData.sceneMinV3.x;
            mapData.sceneMapPoint = (mapData.sceneMaxV3 + mapData.sceneMinV3) / 2;
        }
        else
        {
            Debug.LogWarning("<color=#ff0800>OH SHIT NO SET MAP POSITION!!!</color>");
            return;
        }


        foreach (Transform child in this.gameObject.GetComponentsInChildren<Transform>(true))
        {
            if (mapData.mapImage != null && mapData.maskRect != null && mapData.mapCanvasRect != null)
            {
                break;
            }

            switch (child.gameObject.name)
            {
                case "map":
                    mapData.mapRect = child.GetComponent<RectTransform>();
                    mapData.mapImage = child.GetComponent<Image>();
                    if (mapData.mapSprite == null)
                    {
                        mapData.mapImage.sprite = mapData.mapSprite[0];
                        child.GetComponent<Image>().SetNativeSize();
                    }

                    break;
                case "mask":
                    mapData.maskRect = child.GetComponent<RectTransform>();
                    break;
                case "GameMap":
                    mapData.mapCanvasRect = child.GetComponent<RectTransform>();
                    if (child.TryGetComponent(out CanvasGroup group))
                    {
                        mapData.mapGroupAlpha = group;
                    }

                    //mapData.mapGroupAlpha = child.GetComponent<CanvasGroup>();
                    break;
            }
        }

        MapSwitch(mapInfo.GameStartMod);

        if (mapInfo.GameStartMod == EMapMode.EBigMap)
        {
            mapInfo.maskRect.localRotation = Quaternion.Euler(0, 0, 0);
            mapInfo.zoomValue = 1;
        }
    }

    #endregion


    #region MapFunction

    /// <summary>
    /// 地图更新(表现层)
    /// </summary>
    /// <param name="PlayerPos"></param>
    public void MapUpdate(Transform PlayerPos)
    {
        if (mapInfo != null)
        {
            if (mapInfo.TrackPlayers)
            {
                MapPosTrackTargetPos(mapInfo, PlayerPos.position);
            }

            if (mapInfo.switchMapType)
            {
                if (mapInfo.GameStartMod == EMapMode.EMiniMap)
                {
                    MapSwitch(mapInfo.GameStartMod = EMapMode.EBigMap);
                }
                else if (mapInfo.GameStartMod == EMapMode.EBigMap)
                {
                    MapSwitch(mapInfo.GameStartMod = EMapMode.EMiniMap);
                }

                mapInfo.switchMapType = false;
                MapMod = mapInfo.GameStartMod;
            }

            if (mapInfo.MapElements != null)
            {
                foreach (var actor in mapInfo.MapElements)
                {
                    if (actor.Actor != null && actor.RuntimeMark != null)
                    {
                        IconPosSet(mapInfo, actor.Actor.transform, actor.RuntimeMark.GetComponent<RectTransform>());
                        IconSpin(actor.RuntimeMark.GetComponent<RectTransform>(),
                            -actor.Actor.transform.eulerAngles.y);
                    }
                }
            }

            if (mapInfo.CanZoomMap)
            {
                GameMapUtilities.MapZoomMultiply(mapInfo.maskRect, mapInfo.mapRect, mapInfo.zoomValue,
                    mapInfo.zoomMax, mapInfo.zoomMin);
            }


            if (mapInfo.SpinMap)
            {
                IconSpin(mapInfo.maskRect, -PlayerPos.eulerAngles.y);
            }

            if (mapInfo.floor.Length > 1)
            {
                MapImageSwitch(mapInfo.floor, mapInfo.mapSprite, PlayerPos, mapInfo.mapImage);
            }
        }
    }

    /// <summary>
    /// 刷新地图数据
    /// </summary>
    public void RefreshMap()
    {
        if (mapInfo.MapElements != null && mapInfo.MapElements.Count > 1)
        {
            for (int i = mapInfo.MapElements.Count - 1; i >= 0; i--)
            {
                if (mapInfo.MapElements[i] == null || mapInfo.MapElements[i].Actor == null)
                    // || !mapInfo.MapElements[i].Actor.Features.TryGetFeature<MapMark>(out var mark) || !mark.Enabled)
                {
                    Destroy(mapInfo.MapElements[i].RuntimeMark);
                    mapInfo.MapElements.RemoveAt(i);
                }
            }
        }

        //GameLevel.Current.Actors.Actors
        var actors = GameObject.FindObjectsOfType<MapMark>();
        foreach (var actor in actors)
        {
            bool alreadyIn = false;
            foreach (var element in mapInfo.MapElements)
            {
                if (element.Actor == actor)
                {
                    alreadyIn = true;
                    break;
                }
            }

            // if (!alreadyIn && actor.Features.TryGetFeature<MapMark>(out var mark) && mark.Enabled)
            // {
            //     var id = actor.IsLocal ? mark.AsSelfIconId : mark.AsOtherIconId;
            //     var size = mark.IconSize;
            //     AddElement(actor, id, size);
            // }
        }
    }

    /// <summary>
    /// 添加图标信息
    /// </summary>
    /// <param name="actor"></param>
    /// <param name="iconIndex"></param>
    public void AddElement(MapMark  actor, int iconIndex, float size)
    {
        if (iconIndex > mapInfo.Icons.Count - 1)
        {
            Debug.LogWarning("<color=#ff0880>Can't find icon!!!!! \r\n pls check feature MapMark Icon id </color>");
            return;
        }

        if (mapInfo.Icons[iconIndex] == null)
        {
            var icon = new GameObject();
            icon.name = "Icon_" + iconIndex;
            icon.transform.parent = mapInfo.mapImage.transform;
            icon.AddComponent<Image>().sprite = null;
            icon.GetComponent<Image>().color = Color.clear;
            mapInfo.MapElements.Add(new MapElement
                {Actor = actor, Icon = mapInfo.Icons[iconIndex], RuntimeMark = icon});
        }
        else
        {
            var icon = new GameObject();
            icon.name = "Icon_" + iconIndex;
            icon.transform.parent = mapInfo.mapImage.transform;
            icon.AddComponent<Image>().sprite = mapInfo.Icons[iconIndex];
            var iconRect = icon.GetComponent<RectTransform>();
            RectReset(iconRect);
            icon.GetComponent<Image>().SetNativeSize();
            iconRect.sizeDelta = new Vector2(iconRect.sizeDelta.x * size, iconRect.sizeDelta.y * size);
            mapInfo.MapElements.Add(new MapElement
                {Actor = actor, Icon = mapInfo.Icons[iconIndex], RuntimeMark = icon});
        }
    }

    /// <summary>
    /// 清理场景
    /// </summary>
    /// <param name="mapInfo"></param>
    public void ClearMapIcon(GameMapData mapInfo)
    {
        if (mapInfo.MapElements != null)
        {
            mapInfo.MapElements.Clear();
        }

        if (mapInfo.mapRect.transform.childCount > 0)
        {
            for (int i = 0; i < mapInfo.mapRect.transform.childCount; i++)
            {
                Destroy(mapInfo.mapRect.transform.GetChild(0).gameObject);
            }
        }
    }

    /// <summary>
    /// 切换地图模式
    /// </summary>
    /// <param name="nowMode"></param>
    public void MapSwitch(EMapMode nowMode)
    {
        switch (nowMode)
        {
            case EMapMode.EBigMap:
                mapInfo.mapCanvasRect.anchorMax = mapInfo.BigMapAnchorPos;
                mapInfo.mapCanvasRect.anchorMin = mapInfo.BigMapAnchorPos;
                mapInfo.mapCanvasRect.pivot = mapInfo.BigMapAnchorPos;
                mapInfo.mapCanvasRect.sizeDelta = mapInfo.BigMapSize;
                mapInfo.mapCanvasRect.anchoredPosition = mapInfo.BigMapPos;
                mapInfo.maskRect.localRotation = Quaternion.Euler(0, 0, 0);
                mapInfo.zoomValue = mapInfo.BigMapScale;
                break;
            case EMapMode.EMiniMap:
                mapInfo.mapCanvasRect.anchorMax = mapInfo.MiniMapAnchorPos;
                mapInfo.mapCanvasRect.anchorMin = mapInfo.MiniMapAnchorPos;
                mapInfo.mapCanvasRect.pivot = mapInfo.MiniMapAnchorPos;
                mapInfo.mapCanvasRect.sizeDelta = mapInfo.MiniMapSize;
                mapInfo.mapCanvasRect.anchoredPosition = mapInfo.MiniMapPos;
                mapInfo.zoomValue = mapInfo.MiniMapScale;
                break;
        }
    }

    /// <summary>
    /// Icon Spin
    /// 图标旋转
    /// </summary>
    /// <param name="icon"></param>
    /// <param name="angle"></param>
    public void IconSpin(RectTransform icon, float angle = 0f)
    {
        var temp_Spin_Value = new Vector3();
        temp_Spin_Value.x = 0;
        temp_Spin_Value.y = 0;
        temp_Spin_Value.z = angle;
        icon.localRotation = Quaternion.Euler(temp_Spin_Value);
    }


    /// <summary>
    /// Icon Track Target position 
    /// 图标定位
    /// </summary>
    /// <param name="scene_Long"></param>
    /// <param name="scene_High"></param>
    /// <param name="scene_Map_Point"></param>
    /// <param name="map"></param>
    /// <param name="player"></param>
    /// <param name="playerIcon"></param>
    public void IconPosSet(GameMapData mapData, Transform player, RectTransform playerIcon)
    {
        var temp_player_pos_1 = new Vector3();
        var temp_player_pos_2 = player.position - mapData.sceneMapPoint;
        temp_player_pos_1.x = Mathf.Clamp((temp_player_pos_2.x / mapData.sceneSize.x * mapData.mapRect.rect.width),
            -mapData.mapRect.rect.width / 2, mapData.mapRect.rect.width / 2);
        temp_player_pos_1.y = Mathf.Clamp((temp_player_pos_2.z / mapData.sceneSize.y * mapData.mapRect.rect.height),
            -mapData.mapRect.rect.height / 2, mapData.mapRect.rect.height / 2);
        playerIcon.localPosition = temp_player_pos_1;
    }

    /// <summary>
    /// Map Track Target position 
    /// 地图定位
    /// </summary>
    /// <param name="scene_Long"></param>
    /// <param name="scene_High"></param>
    /// <param name="scene_Map_Point"></param>
    /// <param name="player"></param>
    /// <param name="map"></param>
    /// <param name="map_Mask"></param>
    public void MapPosTrackTargetPos(GameMapData mapData, Vector3 player)
    {
        var temp_map_pos = new Vector3();
        var temp_player_pos_2 = player - mapData.sceneMapPoint;
        temp_map_pos.x =
            Mathf.Clamp((-temp_player_pos_2.x / mapData.sceneSize.x * mapData.mapRect.rect.width),
                -((mapData.mapRect.rect.width / 2) - (mapData.maskRect.rect.width / 2)),
                (mapData.mapRect.rect.width / 2) - (mapData.maskRect.rect.width / 2));
        temp_map_pos.y =
            Mathf.Clamp((-temp_player_pos_2.z / mapData.sceneSize.y * mapData.mapRect.rect.height),
                -((mapData.mapRect.rect.height / 2) - (mapData.maskRect.rect.height / 2)),
                (mapData.mapRect.rect.height / 2) - (mapData.maskRect.rect.height / 2));
        mapData.mapRect.localPosition = temp_map_pos;
    }

    /// <summary>
    /// 切换地图贴图
    /// </summary>
    /// <param name="floor"></param>
    /// <param name="imagelist"></param>
    /// <param name="player"></param>
    /// <param name="map_Image"></param>
    public void MapImageSwitch(float[] floor, Sprite[] imagelist, Transform player, Image map_Image)
    {
        for (int i = floor.Length - 1; i > -1; i--)
        {
            if (player.position.y >= floor[i])
            {
                map_Image.sprite = imagelist[i];
                break;
            }
        }
    }

    #endregion

    /// <summary>
    /// 图片Reset
    /// </summary>
    /// <param name="resetRect"></param>
    public void RectReset(RectTransform resetRect)
    {
        resetRect.localRotation = new Quaternion(0f, 0f, 0f, 1f);
        resetRect.localPosition = Vector3.zero;
        resetRect.localScale = Vector3.one;
        resetRect.anchorMin = new Vector2(0.5f, 0.5f);
        resetRect.anchorMax = new Vector2(0.5f, 0.5f);
        resetRect.anchoredPosition = Vector2.zero;
        resetRect.sizeDelta = new Vector2(100f, 100f);
        resetRect.pivot = new Vector2(0.5f, 0.5f);
    }
}


public class GameMapUtilities
{
    #region Map Function

    /// <summary>
    /// Map zoom(Addition)
    /// 缩放(加法)
    /// </summary>
    /// <param name="map_Mask"></param>
    /// <param name="map"></param>
    /// <param name="_offset"></param>
    public static void MapZoomAddition(RectTransform map_Mask, RectTransform map, float _offset)
    {
        var temp_Zoom_Value = new Vector3();
        temp_Zoom_Value.x = map.rect.width + _offset;
        temp_Zoom_Value.y = map.rect.height + _offset;
        map.sizeDelta = temp_Zoom_Value;
    }

    public static void MapZoomAddition(RectTransform map_Mask, RectTransform map, float _offset, float _maxMulitple,
        float _minMulitple = 1f)
    {
        var temp_Zoom_Value = new Vector3();
        temp_Zoom_Value.x = Mathf.Clamp(map.rect.width + _offset, map_Mask.rect.width * _minMulitple,
            map_Mask.rect.width * _maxMulitple);
        temp_Zoom_Value.y = Mathf.Clamp(map.rect.height + _offset, map_Mask.rect.height * _minMulitple,
            map_Mask.rect.height * _maxMulitple);
        map.sizeDelta = temp_Zoom_Value;
    }

    /// <summary>
    /// Map zoom(Multiply)
    /// 缩放(乘法)
    /// </summary>
    /// <param name="map_Mask"></param>
    /// <param name="map"></param>
    /// <param name="_offset"></param>
    public static void MapZoomMultiply(RectTransform map_Mask, RectTransform map, float _offset)
    {
        var temp_Zoom_Value = new Vector3();
        temp_Zoom_Value.x = map_Mask.rect.width * _offset;
        temp_Zoom_Value.y = map_Mask.rect.height * _offset;
        map.sizeDelta = temp_Zoom_Value;
    }

    public static void MapZoomMultiply(RectTransform map_Mask, RectTransform map, float _offset, float _maxMulitple,
        float _minMulitple = 1f)
    {
        var temp_Zoom_Value = new Vector3();
        temp_Zoom_Value.x =
            Mathf.Clamp(map_Mask.rect.width * _offset, map_Mask.rect.width * _minMulitple,
                map_Mask.rect.width * _maxMulitple);
        temp_Zoom_Value.y =
            Mathf.Clamp(map_Mask.rect.height * _offset, map_Mask.rect.height * _minMulitple,
                map_Mask.rect.height * _maxMulitple);
        map.sizeDelta = temp_Zoom_Value;
    }

    /// <summary>
    /// 移动地图
    /// </summary>
    /// <param name="mapData"></param>
    /// <param name="moveTemp"></param>
    public static void MapMove(GameMapData mapData, Vector3 moveTemp)
    {
        mapData.mapRect.localPosition += moveTemp;
    }

    #endregion
}


[Serializable]
public class GameMapData
{
    public Transform sceneMax;
    public Vector3 sceneMaxV3;
    public Transform sceneMin;
    public Vector3 sceneMinV3;

    [Range(1, 10)] public float zoomMax = 5f;
    [Range(1, 10)] public float zoomMin = 1f;
    public float _zoomValue = 2.5f; //缩放值

    public float zoomValue
    {
        get { return _zoomValue; }
        set { _zoomValue = Mathf.Clamp(value, zoomMin, zoomMax); }
    }

    public bool CanZoomMap = true;
    public bool SpinMap;
    public bool TrackPlayers = true;

    [HideInInspector] public Vector2 sceneSize; //场景地图实际大小（不需要赋初始值，会根据sceneMax和sceneMin计算）
    [HideInInspector] public Vector3 sceneMapPoint; //本地场景中心坐标（不需要赋初始值，会根据sceneMax和sceneMin计算）
    [HideInInspector] public RectTransform maskRect; //地图遮罩坐标
    [HideInInspector] public RectTransform mapRect; //地图背景坐标
    [HideInInspector] public RectTransform mapCanvasRect; //地图obj（不是Prefab）
    [HideInInspector] public Image mapImage; //地图物体

    [HideInInspector] public EMapMode GameStartMod = 0;
    [HideInInspector] public bool switchMapType;

    //让玩家配置地图楼层
    public float[] floor;

    //让玩家配置地图图片
    public Sprite[] mapSprite;

    //让玩家配置一共有哪些Icon
    public List<Sprite> Icons;

    private List<MapElement> _mapElements;

    //运行时的数据
    [HideInInspector]
    public List<MapElement> MapElements
    {
        get
        {
            if (_mapElements == null)
                _mapElements = new List<MapElement>();
            return _mapElements;
        }
        set { _mapElements = value; }
    }

    //小地图屏幕锚点位置
    public Vector2 MiniMapAnchorPos = new Vector2(1f, 1f);

    //大地图屏幕锚点位置
    public Vector2 BigMapAnchorPos = new Vector2(0.5f, 0.5f);

    //小地图位置
    public Vector2 MiniMapPos = new Vector2(0, 0);

    //大地图位置
    public Vector2 BigMapPos = new Vector2(0, 0);

    //小地图大小
    public Vector2 MiniMapSize;

    //大地图大小
    public Vector2 BigMapSize;

    //小地图默认缩放比
    public float MiniMapScale = 2f;

    //大地图默认缩放比
    public float BigMapScale = 1f;

    public float alpha = 1;

    [HideInInspector] public CanvasGroup mapGroupAlpha;
}

public class MapElement
{
    public MapMark  Actor;
    public Sprite Icon;
    public GameObject RuntimeMark;
}

[Serializable]
public enum EMapMode
{
    EMiniMap = 0,
    EBigMap = 1
}

[Serializable]
public class MapMark : MonoBehaviour 
{
    public int AsSelfIconId;
    public int AsOtherIconId;
    public string objName;
    [Range(1, 0)]
    public float IconSize = 1;
    public bool Enabled;
}