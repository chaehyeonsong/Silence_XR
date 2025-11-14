using UnityEngine;
using UnityEngine.Video;

public class OpeningVideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;      // VideoPlayer 컴포넌트
    public GameObject openingCanvas;     // 오프닝용 Canvas (OpeningCanvas)

    void Start()
    {
        // 씬이 시작될 때 오프닝 캔버스를 켜고 영상 재생
        if (openingCanvas != null)
            openingCanvas.SetActive(true);

        if (videoPlayer != null)
        {
            // 영상 끝났을 때 호출될 콜백 등록
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Play();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        // 영상 끝나면 오프닝 캔버스 끄기
        if (openingCanvas != null)
            openingCanvas.SetActive(false);

        // 더 이상 필요 없으면 이벤트 해제
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }

    // 옵션: 키 입력으로 스킵하고 싶을 때 (예: Space or Trigger)
    void Update()
    {
        // 테스트용: Space 누르면 스킵
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SkipOpening();
        }
    }

    public void SkipOpening()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        if (openingCanvas != null)
            openingCanvas.SetActive(false);
    }
}
