using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongList : MonoBehaviour
{
    public SongManager songManager;

    public StageManager stageManager;

    public GameObject songButtonPrefab;

    private bool isLoaded = false;

    public GameObject contentObject;
    public List<GameObject> songNodeList;
    
    public float listPadding = 20;

    // Start is called before the first frame update
    void Start()
    {
        contentObject.GetComponent<VerticalLayoutGroup>().spacing = listPadding;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isLoaded)
        {
            if (songManager.IsLoaded)
            {
                BuildSongNodeList();
                isLoaded = true;
            }
        }
    }

    void BuildSongNodeList()
    {
        foreach(SongInfo songInfo in songManager.songList)
        {
            GameObject newGameObject = Instantiate(songButtonPrefab, contentObject.transform);
            
            TextMeshProUGUI textComp = newGameObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            textComp.SetText(songInfo.Name);
            
            Button buttonComp = newGameObject.GetComponentInChildren<Button>();
            buttonComp.onClick.AddListener(() => stageManager.PlaySong(songInfo, songInfo.DifficultyList.Count - 1, true));
        }
    }
}
