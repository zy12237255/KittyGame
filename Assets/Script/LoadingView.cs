using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image valueBar;

    [Header("Loading Settings")]
    [SerializeField] private string targetSceneName = "Game";
    [SerializeField] private float minLoadingTime = 1.5f;

    private AsyncOperation asyncOperation;

    private void Awake()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (valueBar == null)
        {
            Transform barTransform = transform.Find("Content/ProgressBar/valueBar");
            if (barTransform == null) barTransform = transform.Find("ProgressBar/valueBar");
            if (barTransform == null) barTransform = transform.Find("valueBar");
            if (barTransform != null) valueBar = barTransform.GetComponent<Image>();
        }

        if (valueBar != null)
        {
            valueBar.type = Image.Type.Filled;
            valueBar.fillAmount = 0f;
        }
    }

    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Loading" || sceneName.Contains("Loading"))
            Show();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        StartCoroutine(LoadSceneCoroutine());
    }

    private IEnumerator LoadSceneCoroutine()
    {
        if (valueBar != null)
            valueBar.fillAmount = 0f;

        float beginTime = Time.time;
        asyncOperation = SceneManager.LoadSceneAsync(targetSceneName);
        asyncOperation.allowSceneActivation = false;

        while (asyncOperation.progress < 0.9f)
        {
            float progress = asyncOperation.progress / 0.9f; // Normalize to 0-1
            if (valueBar != null)
                valueBar.fillAmount = progress;
            yield return null;
        }

        float elapsed = Time.time - beginTime;
        if (elapsed < minLoadingTime)
        {
            float initialFill = valueBar != null ? valueBar.fillAmount : 0f;
            while (elapsed < minLoadingTime)
            {
                elapsed = Time.time - beginTime;
                float t = elapsed / minLoadingTime;
                float progress = Mathf.Lerp(initialFill, 1f, t);
                if (valueBar != null)
                    valueBar.fillAmount = progress;
                yield return null;
            }
        }

        if (valueBar != null)
            valueBar.fillAmount = 1f;

        yield return null;
        asyncOperation.allowSceneActivation = true;
    }

    public void Init()
    {
        if (valueBar != null)
            valueBar.fillAmount = 0f;
    }

    public Image ValueBar => valueBar;
}

