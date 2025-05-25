using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Relics; // Ensure this is present
#if UNITY_EDITOR
using UnityEditor; // Ensure this is present
#endif

/// <summary>
/// Test script for validating relic timing mechanisms.
/// This script provides debugging tools and test scenarios for the enhanced timing system.
/// </summary>
public class RelicTimingTest : MonoBehaviour
{
    [Header("Test Configuration")]
    public bool enableDebugUI = true;
    public bool autoRunTests = false;
    public float testInterval = 5f;
    
    [Header("Test Relics")]
    public List<string> testRelicIds = new List<string>
    {
        "retaliation",           // next-spell timing
        "stillness",             // move timing  
        "wave_walkers_boon",     // duration timing
        "battle_trance"          // wave-start timing (if implemented)
    };
    
    private PlayerController testPlayer;
    private RelicManager relicManager;
    private List<TestResult> testResults = new List<TestResult>();
    private bool isRunningTests = false;
    
    [System.Serializable]
    public class TestResult
    {
        public string testName;
        public bool passed;
        public string details;
        public float timestamp;
    }
    
    private void Start()
    {
        // Find required components
        testPlayer = FindFirstObjectByType<PlayerController>();
        relicManager = RelicManager.Instance;
        
        if (testPlayer == null)
        {
            Debug.LogError("[RelicTimingTest] No PlayerController found in scene");
            return;
        }
        
        if (relicManager == null)
        {
            Debug.LogError("[RelicTimingTest] No RelicManager found in scene");
            return;
        }
        
        Debug.Log("[RelicTimingTest] Timing test system initialized");
        
        if (autoRunTests)
        {
            StartCoroutine(RunAutomaticTests());
        }
    }
    
    private System.Collections.IEnumerator RunAutomaticTests()
    {
        yield return new WaitForSeconds(2f); // Wait for system to initialize
        
        while (true)
        {
            if (!isRunningTests)
            {
                StartCoroutine(RunTimingTests());
            }
            yield return new WaitForSeconds(testInterval);
        }
    }
    
    public void StartTimingTests()
    {
        if (!isRunningTests)
        {
            StartCoroutine(RunTimingTests());
        }
    }
    
    private System.Collections.IEnumerator RunTimingTests()
    {
        isRunningTests = true;
        Debug.Log("[RelicTimingTest] Starting comprehensive timing tests...");
        
        // Test 1: Duration-based timing
        yield return StartCoroutine(TestDurationTiming());
        
        // Test 2: Move-based timing
        yield return StartCoroutine(TestMoveTiming());
        
        // Test 3: Next-spell timing
        yield return StartCoroutine(TestNextSpellTiming());
        
        // Test 4: Basic timing constraint parsing
        yield return StartCoroutine(TestTimingParsing());
        
        Debug.Log("[RelicTimingTest] All timing tests completed");
        PrintTestResults();
        
        isRunningTests = false;
    }
    
    private System.Collections.IEnumerator TestDurationTiming()
    {
        Debug.Log("[RelicTimingTest] Testing duration-based timing...");
        
        // Create a test effect with duration timing
        var testEffect = CreateTestEffect("temporary-speed-boost", "1.5", "duration 3");
        
        if (testEffect == null)
        {
            AddTestResult("Duration Timing", false, "Failed to create test effect");
            yield break;
        }
        
        // Apply the effect
        testEffect.ApplyEffect();
        testEffect.Activate();
        
        bool wasActive = testEffect.IsActive();
        AddTestResult("Duration Timing - Activation", wasActive, $"Effect active: {wasActive}");
        
        // Wait for duration to expire
        yield return new WaitForSeconds(3.5f);
        
        bool stillActive = testEffect.IsActive();
        AddTestResult("Duration Timing - Expiration", !stillActive, $"Effect still active after duration: {stillActive}");
        
        CleanupTestEffect(testEffect);
    }
    
    private System.Collections.IEnumerator TestMoveTiming()
    {
        Debug.Log("[RelicTimingTest] Testing move-based timing...");
        
        var testEffect = CreateTestEffect("temporary-damage-boost", "2", "move");
        
        if (testEffect == null)
        {
            AddTestResult("Move Timing", false, "Failed to create test effect");
            yield break;
        }
        
        Vector3 originalPosition = testPlayer.transform.position;
        
        // Apply the effect
        testEffect.ApplyEffect();
        testEffect.Activate();
        
        bool wasActive = testEffect.IsActive();
        AddTestResult("Move Timing - Activation", wasActive, $"Effect active: {wasActive}");
        
        // Simulate player movement
        testPlayer.transform.position = originalPosition + Vector3.right * 2f;
        
        // Wait a frame for the movement check
        yield return new WaitForSeconds(0.2f);
        
        bool stillActiveAfterMove = testEffect.IsActive();
        AddTestResult("Move Timing - Movement Detection", !stillActiveAfterMove, $"Effect active after movement: {stillActiveAfterMove}");
        
        // Restore original position
        testPlayer.transform.position = originalPosition;
        CleanupTestEffect(testEffect);
    }
    
    private System.Collections.IEnumerator TestNextSpellTiming()
    {
        Debug.Log("[RelicTimingTest] Testing next-spell timing...");
        
        Relics.RelicEffect testEffect = CreateTestEffect("gain-spellpower", "50", "next-spell"); // Use Relics.RelicEffect
        
        if (testEffect == null)
        {
            AddTestResult("Next-Spell Timing", false, "Failed to create test effect");
            yield break;
        }
        
        // Apply the effect
        testEffect.ApplyEffect();
        testEffect.Activate();
        
        bool wasActive = testEffect.IsActive();
        AddTestResult("Next-Spell Timing - Activation", wasActive, $"Effect active: {wasActive}");
        
        // Simulate spell cast
        testEffect.OnSpellCast(); // This should now work
        
        // Wait a moment for the event handling
        yield return new WaitForSeconds(0.1f);
        
        // Simulate damage dealt (which should trigger effect removal)
        if (EventBus.Instance != null)
        {
            var dummyDamage = new Damage(10, Damage.Type.PHYSICAL); // MODIFIED: Correct Damage constructor
            GameObject dummyTargetObj = null;
            Hittable dummyHittable = null;
            try
            {
                dummyTargetObj = new GameObject("DummyTargetForTest");
                dummyHittable = new Hittable(100, Hittable.Team.NEUTRAL, dummyTargetObj); // MODIFIED: Instantiate Hittable
                EventBus.Instance.NotifyPlayerDealtDamage(dummyHittable, dummyDamage); // MODIFIED: Use Notifier
            }
            finally
            {
                if (dummyTargetObj != null)
                {
                    Destroy(dummyTargetObj);
                }
            }
        }
        
        yield return new WaitForSeconds(0.1f);
        
        bool stillActiveAfterSpell = testEffect.IsActive();
        AddTestResult("Next-Spell Timing - Spell Cast", !stillActiveAfterSpell, $"Effect active after spell cast: {stillActiveAfterSpell}");
        
        CleanupTestEffect(testEffect);
    }
    
    private System.Collections.IEnumerator TestTimingParsing()
    {
        Debug.Log("[RelicTimingTest] Testing timing constraint parsing...");
        
        // Test different timing constraint formats
        var testCases = new[]
        {
            new { until = "next-spell", expectedType = "next-spell", expectedDuration = 0f },
            new { until = "move", expectedType = "move", expectedDuration = 0f },
            new { until = "duration 5", expectedType = "duration", expectedDuration = 5f },
            new { until = "duration 10.5", expectedType = "duration", expectedDuration = 10.5f },
            new { until = "wave-start", expectedType = "wave-start", expectedDuration = 0f }
        };
        
        bool allParsingTestsPassed = true;
        
        foreach (var testCase in testCases)
        {
            // Use Relics.RelicEffect for the variable 'effect'
            Relics.RelicEffect effect = CreateTestEffect("temporary-invulnerability", "1", testCase.until);
            if (effect != null)
            {
                // Access protected fields through reflection for testing
                var hasConstraint = GetProtectedField<bool>(effect, "hasTimingConstraint");
                var timingType = GetProtectedField<string>(effect, "timingType");
                var timingDuration = GetProtectedField<float>(effect, "timingDuration");
                
                bool typeMatch = timingType == testCase.expectedType;
                bool durationMatch = Mathf.Approximately(timingDuration, testCase.expectedDuration);
                
                if (!hasConstraint || !typeMatch || !durationMatch)
                {
                    allParsingTestsPassed = false;
                    Debug.LogWarning($"[RelicTimingTest] Parsing test failed for '{testCase.until}': " +
                                   $"hasConstraint={hasConstraint}, type={timingType} (expected {testCase.expectedType}), " +
                                   $"duration={timingDuration} (expected {testCase.expectedDuration})");
                }
                
                CleanupTestEffect(effect);
            }
            else
            {
                allParsingTestsPassed = false;
            }
        }
        
        AddTestResult("Timing Parsing", allParsingTestsPassed, $"All parsing tests passed: {allParsingTestsPassed}");
        yield return null;
    }
    
    private Relics.RelicEffect CreateTestEffect(string effectType, string amount, string until) // Return Relics.RelicEffect
    {
        try
        {
            // Create test effect data
            var effectData = new RelicJsonData.EffectData
            {
                type = effectType,
                amount = amount,
                until = until
            };
            
            // Create test relic
            var relicJson = new RelicJsonData(); 
            relicJson.name = $"Test_{effectType}_{until}";
            relicJson.icon_id = 0; 
            
            var testRelic = new Relic(relicJson, testPlayer);
            
            // Create the effect component
            Relics.RelicEffect effect = null; // Use Relics.RelicEffect
            switch (effectType)
            {
                case "temporary-speed-boost":
                    effect = testPlayer.gameObject.AddComponent<Relics.TemporarySpeedBoostEffect>();
                    break;
                case "temporary-damage-boost":
                    effect = testPlayer.gameObject.AddComponent<Relics.TemporaryDamageBoostEffect>();
                    break;
                case "temporary-invulnerability":
                    effect = testPlayer.gameObject.AddComponent<Relics.TemporaryInvulnerabilityEffect>();
                    break;
                case "gain-spellpower":
                    effect = testPlayer.gameObject.AddComponent<Relics.GainSpellpowerEffect>();
                    break;
                default:
                    Debug.LogWarning($"[RelicTimingTest] Unknown effect type: {effectType}");
                    return null;
            }
            
            if (effect != null)
            {
                effect.Initialize(testRelic, testPlayer, effectData);
                Debug.Log($"[RelicTimingTest] Created test effect: {effectType} with timing: {until}");
            }
            
            return effect;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RelicTimingTest] Failed to create test effect: {e.Message}");
            return null;
        }
    }
    
    private void CleanupTestEffect(Relics.RelicEffect effect) // Parameter is Relics.RelicEffect
    {
        if (effect != null)
        {
            effect.RemoveEffect();
            DestroyImmediate(effect); // Should work if effect is a MonoBehaviour
        }
    }
    
    private T GetProtectedField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (T)field.GetValue(obj);
        }
        return default(T);
    }
    
    private void AddTestResult(string testName, bool passed, string details)
    {
        var result = new TestResult
        {
            testName = testName,
            passed = passed,
            details = details,
            timestamp = Time.time
        };
        
        testResults.Add(result);
        
        string status = passed ? "PASSED" : "FAILED";
        Debug.Log($"[RelicTimingTest] {status}: {testName} - {details}");
    }
    
    private void PrintTestResults()
    {
        Debug.Log("[RelicTimingTest] === TEST RESULTS SUMMARY ===");
        int passed = 0;
        int total = testResults.Count;
        
        foreach (var result in testResults)
        {
            if (result.passed)
            {
                passed++;
            }
        }
        
        Debug.Log($"[RelicTimingTest] {passed} out of {total} tests passed.");
    }
    
#if UNITY_EDITOR
    private void OnGUI()
    {
        if (enableDebugUI)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 500));
            GUILayout.Label("Relic Timing Test", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Run Timing Tests"))
            {
                StartTimingTests();
            }
            
            if (GUILayout.Button("Clear Test Results"))
            {
                testResults.Clear();
                Debug.Log("[RelicTimingTest] Test results cleared");
            }
            
            GUILayout.Label("Test Results:", EditorStyles.boldLabel);
            foreach (var result in testResults)
            {
                string status = result.passed ? "PASSED" : "FAILED";
                GUILayout.Label($"{status}: {result.testName} - {result.details}");
            }
            
            GUILayout.EndArea();
        }
    }
#endif
}
