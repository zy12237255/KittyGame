using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image valueBar; // Progress bar (fillAmount 0->1)
    
    [Header("Loading Settings")]
    [SerializeField] private string targetSceneName = "Game"; // Scene to load
    [SerializeField] private float minLoadingTime = 1.5f; // Minimum loading time in seconds
    
    private AsyncOperation asyncOperation;
    
    private void Awake()
    {
        AutoAssignReferences();
    }
    
    /// <summary>
    /// Automatically assign UI references
    /// </summary>
    private void AutoAssignReferences()
    {
        // Find valueBar if not assigned
        if (valueBar == null)
        {
            // Try to find in common paths
            Transform valueBarTransform = transform.Find("Content/ProgressBar/valueBar");
            if (valueBarTransform == null)
            {
                valueBarTransform = transform.Find("ProgressBar/valueBar");
            }
            if (valueBarTransform == null)
            {
                valueBarTransform = transform.Find("valueBar");
            }
            
            if (valueBarTransform != null)
            {
                valueBar = valueBarTransform.GetComponent<Image>();
            }
        }
        
        // Initialize progress bar
        if (valueBar != null)
        {
            valueBar.type = Image.Type.Filled;
            valueBar.fillAmount = 0f;
        }
    }
    
    /// <summary>
    /// Register this panel to UIManager and start loading if in Loading scene
    /// </summary>
    private void Start()
    {
        
        // Automatically start loading if this is the Loading scene
        if (SceneManager.GetActiveScene().name == "Loading" || SceneManager.GetActiveScene().name.Contains("Loading"))
        {
            Show();
        }
    }
    
    /// <summary>
    /// Show loading view and start loading scene
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        StartCoroutine(LoadSceneCoroutine());
    }
    
    /// <summary>
    /// Load scene with progress bar update
    /// </summary>
    private IEnumerator LoadSceneCoroutine()
    {
        // Reset progress bar
        if (valueBar != null)
        {
            valueBar.fillAmount = 0f;
        }
        
        float loadingStartTime = Time.time;
        
        // Start loading scene asynchronously
        asyncOperation = SceneManager.LoadSceneAsync(targetSceneName);
        asyncOperation.allowSceneActivation = false; // Don't activate immediately
        
        // Wait until scene is loaded
        while (asyncOperation.progress < 0.9f)
        {
            // Update progress bar based on loading progress
            float progress = asyncOperation.progress / 0.9f; // Normalize to 0-1
            if (valueBar != null)
            {
                valueBar.fillAmount = progress;
            }
            
            yield return null;
        }
        
        // Ensure minimum loading time
        float elapsedTime = Time.time - loadingStartTime;
        if (elapsedTime < minLoadingTime)
        {
            float remainingTime = minLoadingTime - elapsedTime;
            float startProgress = valueBar != null ? valueBar.fillAmount : 0f;
            
            // Smoothly fill remaining progress during remaining time
            while (elapsedTime < minLoadingTime)
            {
                elapsedTime = Time.time - loadingStartTime;
                float timeProgress = elapsedTime / minLoadingTime;
                float progress = Mathf.Lerp(startProgress, 1f, timeProgress);
                
                if (valueBar != null)
                {
                    valueBar.fillAmount = progress;
                }
                
                yield return null;
            }
        }
        
        // Set progress to 100%
        if (valueBar != null)
        {
            valueBar.fillAmount = 1f;
        }
        
        // Wait a frame to ensure progress bar is fully visible
        yield return null;
        
        // Activate the scene
        asyncOperation.allowSceneActivation = true;
    }
    
    /// <summary>
    /// Initialize LoadingView
    /// </summary>
    public void Init()
    {
        // Reset progress bar
        if (valueBar != null)
        {
            valueBar.fillAmount = 0f;
        }
    }
    
    // Public getters
    public Image ValueBar => valueBar;
}

