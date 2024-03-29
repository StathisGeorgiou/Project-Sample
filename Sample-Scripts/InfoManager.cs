﻿using ARPolis.Map;
using ARPolis.UI;
using StaGeUnityTools;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ARPolis.Data
{
    public class InfoManager : Singleton<InfoManager>
    {
        protected InfoManager(){}

        #region json storage to retrieve data
        public enum LoadJsonFolder { STREAMING_ASSETS, RESOURCES, CUSTOM }
        public LoadJsonFolder jsonFolder = LoadJsonFolder.STREAMING_ASSETS;

        public enum LoadImagesFolder { STREAMING_ASSETS, RESOURCES, CUSTOM }
        public LoadImagesFolder imagesFolder = LoadImagesFolder.STREAMING_ASSETS;

        public enum LoadAudioFolder { STREAMING_ASSETS, RESOURCES, CUSTOM }
        public LoadAudioFolder audioFolder = LoadAudioFolder.STREAMING_ASSETS;
        #endregion

        /// <summary>
        /// due to beta version there is only 1 area
        /// </summary>
        public AreaEntity areaAthens;
        /// <summary>
        /// ids of selected entities
        /// </summary>
        public string areaNowID, topicNowID, tourNowID, poiNowID;

        /// <summary>
        /// safety for one time initialization
        /// </summary>
        private bool hasInit;

        /// <summary>
        /// call at start for instatiation at runtime
        /// using Singleton pattern
        /// </summary>
        public void Init()
        {
            //do not repeat initialization
            if (hasInit) { if (Application.isEditor) Debug.LogWarning("Initializing Aborted!"); return; }
            //select folder to retrieve data
            jsonFolder = LoadJsonFolder.STREAMING_ASSETS;
            //get data
            StartCoroutine(ReadAllData());
            //init completed
            hasInit = true;
        }

        #region global help functions

        /// <summary>
        /// check if any entity has been selected
        /// </summary>
        /// <returns></returns>
        public bool HasDataNow()
        {
            return !string.IsNullOrWhiteSpace(areaNowID) && !string.IsNullOrWhiteSpace(topicNowID)
                    && !string.IsNullOrWhiteSpace(tourNowID) && !string.IsNullOrWhiteSpace(poiNowID);
        }

        /// <summary>
        /// check if this place is saved by user as favourite
        /// </summary>
        /// <param name="userPlace"></param>
        /// <returns></returns>
        public bool IsUserPlaceItemNowExists(out UserPlaceItem userPlace)
        {
            userPlace = null;
            if (!HasDataNow()) return false;

            PoiEntity poi = GetPoiEntity(poiNowID);
            if (poi == null || string.IsNullOrWhiteSpace(poi.id)) return false;

            userPlace = new UserPlaceItem
            {
                placeName = poi.infoEN.name,
                areaID = Instance.areaNowID,
                topicID = Instance.topicNowID,
                tourID = Instance.tourNowID,
                poiID = Instance.poiNowID,

            };
            return true;
        }

        /// <summary>
        /// get selected area
        /// </summary>
        /// <returns></returns>
        public AreaEntity GetActiveArea()
        {
            if (areaNowID == "1") { return areaAthens; }
            return null;
        }

        /// <summary>
        /// get area by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public AreaEntity GetAreaWithID(string id)
        {
            if (id == "1") { return areaAthens; }
            return null;
        }

        /// <summary>
        /// get selected topic
        /// </summary>
        /// <returns></returns>
        public TopicEntity GetActiveTopic()
        {
            return GetActiveArea().topics.Find(b => b.id == topicNowID);
        }

        /// <summary>
        /// get topic tours length
        /// </summary>
        /// <param name="topicID"></param>
        /// <returns></returns>
        public int GetTopicToursLength(int topicID)
        {
            return areaAthens.topics[topicID].tours.Count;
        }

        /// <summary>
        /// get poi by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public PoiEntity GetPoiEntity(string id)
        {
            return areaAthens.topics.Find(b => b.id == topicNowID).tours.Find(t => t.id == tourNowID).pois.Find(p => p.id == id);
        }

        /// <summary>
        /// check if poi UI entity exists
        /// </summary>
        /// <param name="poiID"></param>
        /// <param name="tourID"></param>
        /// <param name="topicID"></param>
        /// <param name="areaID"></param>
        /// <returns></returns>
        public PoiEntity GetPoiEntityFromMenu(string poiID, string tourID, string topicID, string areaID)
        {
            return GetAreaWithID(areaID).topics.Find(b => b.id == topicID).tours.Find(t => t.id == tourID).pois.Find(p => p.id == poiID);
        }

        /// <summary>
        /// we need all data in order to return back from map properly
        /// </summary>
        /// <param name="poi"></param>
        public void SetInstantlyMyPlaceData(PoiEntity poi)
        {
            if (poi == null || !poi.Exists()) return;
            areaNowID = poi.areaID;
            topicNowID = poi.topicID;
            tourNowID = poi.tourID;
        }

        #endregion

        /// <summary>
        /// show pois on map
        /// when user clicks map button
        /// </summary>
        public void ShowPois()
        {
            MapController mapController = FindObjectOfType<MapController>();
            mapController.ClearMap();
            //get all tours for selected area
            TourEntity tourEntityNow = GetActiveArea().topics.Find(b => b.id == topicNowID).tours.Find(t => t.id == tourNowID);
            List<PoiEntity> poiEntities = tourEntityNow.pois;
            //iterate pois to create clickable markers on map with info data
            for (int i=0; i<poiEntities.Count; i++)
            {
                //skip if pos is null
                if (poiEntities[i].pos == Vector2.zero) continue;
                //create new marker
                CustomMarkerGUI marker = CustomMarkerEngineGUI.AddMarker(poiEntities[i].pos, "");
                marker.transform.name = poiEntities[i].GetTitle();
                //create marker info
                PoiItem poiItem = marker.gameObject.AddComponent<PoiItem>();
                poiItem.poiID = poiEntities[i].id;
                poiItem.poiEntity = poiEntities[i];
                poiItem.Init();
                poiItem.SetImage(StaticData.GetPoiIcon(topicNowID));
            }
            //finally set map zoom so that all markers to be visible on screen
            mapController.ZoomOnMarkers(false);
        }

        /// <summary>
        /// read all data from json files
        /// </summary>
        /// <returns></returns>
        private IEnumerator ReadAllData()
        {
            yield return new WaitForSeconds(0.5f);

            //Athens area - data is constructed here due to beta version
            //this will be replaced by json in future updates
            areaAthens = new AreaEntity();
            areaAthens.id = "1";
            areaAthens.infoGR.name = "Αθήνα";
            areaAthens.infoEN.name = "Athens";
            areaAthens.infoGR.desc = "Ανακαλύψτε τις ιστορίες της Αθήνας μέσα απο διαφορετικές θεματικές ενότητες.";
            areaAthens.infoEN.desc = "Discover the stories of Athens through different thematic units.";
            string folderDataAthens = StaticData.folderJsons + StaticData.folderAthens;

            #region get topics
            string topicsAthens = folderDataAthens + StaticData.jsonTopicsFileURL;
            areaAthens.jsonClassTopics = new List<JsonClassTopic>();
            if (jsonFolder == LoadJsonFolder.RESOURCES)
            {
                areaAthens.jsonClassTopics = StaticData.LoadDataFromJson<JsonClassTopic>(topicsAthens).ToList();
            }
            else
            if (jsonFolder == LoadJsonFolder.STREAMING_ASSETS)
            {
                topicsAthens = Path.Combine(Application.streamingAssetsPath, topicsAthens);
                topicsAthens += ".json";
                
                CoroutineWithData cd = new CoroutineWithData(this, StaticData.LoadJsonData<JsonClassTopic>(topicsAthens));
                yield return cd.Coroutine;
                if (cd.result == null) {
                    Debug.LogWarning("Error reading " + topicsAthens);
                    //stop
                    yield break;
                }
                else {
                    areaAthens.jsonClassTopics = cd.result as List<JsonClassTopic>;

                    areaAthens.InitTopics();
                }
            }
            yield return new WaitForEndOfFrame();
            #endregion

            #region get jsons for all topics

            if (areaAthens.topics.Count > 0)
            {
                for (int i = 0; i < areaAthens.topics.Count; i++)
                {
                    //topic folder url
                    string topicFolder = folderDataAthens + StaticData.FolderTopic(areaAthens.topics[i].id); //Debug.Log(topicFolder);

                    #region get tours 
                    //tours file url
                    string topicToursFolder = topicFolder + StaticData.jsonTourFileURL;
                    //create new list
                    areaAthens.topics[i].jsonClassTours = new List<JsonClassTour>();
                    //fill list
                    if (jsonFolder == LoadJsonFolder.RESOURCES)
                    {
                        areaAthens.topics[i].jsonClassTours = StaticData.LoadDataFromJson<JsonClassTour>(topicToursFolder).ToList();
                    }
                    else
                    if (jsonFolder == LoadJsonFolder.STREAMING_ASSETS)
                    {
                        topicToursFolder = Path.Combine(Application.streamingAssetsPath, topicToursFolder);
                        topicToursFolder += ".json";

                        CoroutineWithData cd = new CoroutineWithData(this, StaticData.LoadJsonData<JsonClassTour>(topicToursFolder));
                        yield return cd.Coroutine;
                        if (cd.result == null) { Debug.LogWarning("Error reading " + topicToursFolder); }
                        else
                        {
                            areaAthens.topics[i].jsonClassTours = cd.result as List<JsonClassTour>;
                        }
                    }

                    #endregion

                    #region get pois 

                    //pois file url
                    string topicPoisFile = topicFolder + StaticData.jsonPOIsFileURL;
                    //create new list
                    areaAthens.topics[i].jsonClassPois = new List<JsonClassPoi>();
                    //fill list
                    if (jsonFolder == LoadJsonFolder.RESOURCES)
                    {
                        areaAthens.topics[i].jsonClassPois = StaticData.LoadDataFromJson<JsonClassPoi>(topicPoisFile).ToList();
                    }
                    else
                    if (jsonFolder == LoadJsonFolder.STREAMING_ASSETS)
                    {
                        topicPoisFile = Path.Combine(Application.streamingAssetsPath, topicPoisFile);
                        topicPoisFile += ".json";

                        CoroutineWithData cd = new CoroutineWithData(this, StaticData.LoadJsonData<JsonClassPoi>(topicPoisFile));
                        yield return cd.Coroutine;
                        if (cd.result == null) { Debug.LogWarning("Error reading " + topicPoisFile); }
                        else
                        {
                            areaAthens.topics[i].jsonClassPois = cd.result as List<JsonClassPoi>;
                        }
                    }

                    #endregion

                    #region get tour-pois connection

                    //pois file url
                    string topicTourPoiFile = topicFolder + StaticData.jsonTourPoiFileURL;
                    //create new list
                    areaAthens.topics[i].jsonClassTourPois = new List<JsonClassTourPoi>();
                    //fill list
                    if (jsonFolder == LoadJsonFolder.RESOURCES)
                    {
                        areaAthens.topics[i].jsonClassTourPois = StaticData.LoadDataFromJson<JsonClassTourPoi>(topicTourPoiFile).ToList();
                    }
                    else
                    if (jsonFolder == LoadJsonFolder.STREAMING_ASSETS)
                    {
                        topicTourPoiFile = Path.Combine(Application.streamingAssetsPath, topicTourPoiFile);
                        topicTourPoiFile += ".json";

                        CoroutineWithData cd = new CoroutineWithData(this, StaticData.LoadJsonData<JsonClassTourPoi>(topicTourPoiFile));
                        yield return cd.Coroutine;
                        if (cd.result == null) { Debug.LogWarning("Error reading " + topicTourPoiFile); }
                        else
                        {
                            areaAthens.topics[i].jsonClassTourPois = cd.result as List<JsonClassTourPoi>;
                        }
                    }

                    #endregion

                    #region get testimonies

                    //pois file url
                    string topicTestimoniesFile = topicFolder + StaticData.jsonTestimoniesFileURL;
                    //create new list
                    areaAthens.topics[i].jsonClassTestimonies = new List<JsonClassTestimony>();
                    //fill list
                    if (jsonFolder == LoadJsonFolder.RESOURCES)
                    {
                        areaAthens.topics[i].jsonClassTestimonies = StaticData.LoadDataFromJson<JsonClassTestimony>(topicTestimoniesFile).ToList();
                    }
                    else
                    if (jsonFolder == LoadJsonFolder.STREAMING_ASSETS)
                    {
                        topicTestimoniesFile = Path.Combine(Application.streamingAssetsPath, topicTestimoniesFile);
                        topicTestimoniesFile += ".json";

                        CoroutineWithData cd = new CoroutineWithData(this, StaticData.LoadJsonData<JsonClassTestimony>(topicTestimoniesFile));
                        yield return cd.Coroutine;
                        if (cd.result == null) { Debug.LogWarning("Error reading " + topicTestimoniesFile); }
                        else
                        {
                            areaAthens.topics[i].jsonClassTestimonies = cd.result as List<JsonClassTestimony>;
                        }
                    }

                    #endregion

                    #region get persons

                    //pois file url
                    string topicPersonsFile = topicFolder + StaticData.jsonPersonsFileURL;
                    //create new list
                    areaAthens.topics[i].jsonClassPersons = new List<JsonClassPerson>();
                    //fill list
                    if (jsonFolder == LoadJsonFolder.RESOURCES)
                    {
                        areaAthens.topics[i].jsonClassPersons = StaticData.LoadDataFromJson<JsonClassPerson>(topicPersonsFile).ToList();
                    }
                    else
                    if (jsonFolder == LoadJsonFolder.STREAMING_ASSETS)
                    {
                        topicPersonsFile = Path.Combine(Application.streamingAssetsPath, topicPersonsFile);
                        topicPersonsFile += ".json";

                        CoroutineWithData cd = new CoroutineWithData(this, StaticData.LoadJsonData<JsonClassPerson>(topicPersonsFile));
                        yield return cd.Coroutine;
                        if (cd.result == null) { Debug.LogWarning("Error reading " + topicPersonsFile); }
                        else
                        {
                            areaAthens.topics[i].jsonClassPersons = cd.result as List<JsonClassPerson>;
                        }
                    }

                    #endregion

                    #region get digital exhibits

                    //pois file url
                    string topicDigitalExhibitsFile = topicFolder + StaticData.jsonDigitalExhibitsFileURL;
                    //create new list
                    areaAthens.topics[i].jsonClassDigitalExhibits = new List<JsonClassDigitalExhibit>();
                    //fill list
                    if (jsonFolder == LoadJsonFolder.RESOURCES)
                    {
                        areaAthens.topics[i].jsonClassDigitalExhibits = StaticData.LoadDataFromJson<JsonClassDigitalExhibit>(topicDigitalExhibitsFile).ToList();
                    }
                    else
                    if (jsonFolder == LoadJsonFolder.STREAMING_ASSETS)
                    {
                        topicDigitalExhibitsFile = Path.Combine(Application.streamingAssetsPath, topicDigitalExhibitsFile);
                        topicDigitalExhibitsFile += ".json";

                        CoroutineWithData cd = new CoroutineWithData(this, StaticData.LoadJsonData<JsonClassDigitalExhibit>(topicDigitalExhibitsFile));
                        yield return cd.Coroutine;
                        if (cd.result == null) { Debug.LogWarning("Error reading " + topicDigitalExhibitsFile); }
                        else
                        {
                            areaAthens.topics[i].jsonClassDigitalExhibits = cd.result as List<JsonClassDigitalExhibit>;
                        }
                    }

                    #endregion

                    #region get events

                    //pois file url
                    string topicEventsFile = topicFolder + StaticData.jsonEventsFileURL;
                    //create new list
                    areaAthens.topics[i].jsonClassEvents = new List<JsonClassEvent>();
                    //fill list
                    if (jsonFolder == LoadJsonFolder.RESOURCES)
                    {
                        areaAthens.topics[i].jsonClassEvents = StaticData.LoadDataFromJson<JsonClassEvent>(topicEventsFile).ToList();
                    }
                    else
                    if (jsonFolder == LoadJsonFolder.STREAMING_ASSETS)
                    {
                        topicEventsFile = Path.Combine(Application.streamingAssetsPath, topicEventsFile);
                        topicEventsFile += ".json";

                        CoroutineWithData cd = new CoroutineWithData(this, StaticData.LoadJsonData<JsonClassEvent>(topicEventsFile));
                        yield return cd.Coroutine;
                        if (cd.result == null) { Debug.LogWarning("Error reading " + topicEventsFile); }
                        else
                        {
                            areaAthens.topics[i].jsonClassEvents = cd.result as List<JsonClassEvent>;
                        }
                    }

                    #endregion

                    areaAthens.topics[i].InitTours(areaAthens.id);
                }
                
                yield return new WaitForEndOfFrame();
            }
            #endregion

            yield break;
        }

        /// <summary>
        /// fix json string
        /// to be used as array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        string FixJsonItems(string value)
        {
            value = "{\"Items\":" + value + "}";
            return value;
        }

    }

}
