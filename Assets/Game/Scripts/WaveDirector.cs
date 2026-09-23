using System;
using UnityEngine;

// Driven by GameManager's match clock; no second timer or scene component.
public class WaveDirector
{
    public enum WaveState { Stopped, Spawning, WaitingForNextWave, Completed }
    public WaveState State { get; private set; }
    public int WaveNumber => plan == null ? 0 : waveIndex + 1;
    public int WaveCount => plan == null ? 0 : plan.WaveCount;
    public int SpawnedThisWave { get; private set; }
    public float SecondsUntilNextWave { get; private set; }
    private WavePlanConfig plan;
    private int waveIndex;
    private float waveStart;
    private float nextSpawnTime;
    private float nextConstantSpawnTime;
    public int ConstantSpawned { get; private set; }

    public void Begin(WavePlanConfig wavePlan)
    {
        if (wavePlan == null) throw new ArgumentNullException(nameof(wavePlan));
        if (!wavePlan.Validate(out string error)) throw new ArgumentException(error, nameof(wavePlan));
        plan = wavePlan;
        nextConstantSpawnTime = 0f;
        ConstantSpawned = 0;
        waveIndex = 0;
        waveStart = nextSpawnTime = 0f;
        SpawnedThisWave = 0;
        SecondsUntilNextWave = plan.GetWave(0).duration;
        State = WaveState.Spawning;
    }

    public void Stop() => State = WaveState.Stopped;

    public void Tick(float elapsed, Func<bool> trySpawn)
    {
        if (State == WaveState.Stopped) return;
        // Independent clock and quota; the shared spawner still enforces the population cap.
        if (plan.BackgroundWave.enabled && elapsed >= nextConstantSpawnTime)
        {
            nextConstantSpawnTime = elapsed + plan.BackgroundWave.spawnInterval;
            if (trySpawn()) ConstantSpawned++;
        }
        if (State == WaveState.Completed) return;
        WavePlanConfig.Wave wave = plan.GetWave(waveIndex);
        // Old quotas expire even when a long frame crosses multiple boundaries.
        while (elapsed >= waveStart + wave.duration)
        {
            waveStart += wave.duration;
            if (waveIndex + 1 >= plan.WaveCount)
            {
                State = WaveState.Completed;
                SecondsUntilNextWave = 0f;
                return;
            }
            wave = plan.GetWave(++waveIndex);
            SpawnedThisWave = 0;
            nextSpawnTime = waveStart;
            State = WaveState.Spawning;
        }
        SecondsUntilNextWave = Mathf.Max(0f, waveStart + wave.duration - elapsed);
        if (State != WaveState.Spawning || elapsed < nextSpawnTime) return;
        // A full population never accumulates a catch-up burst.
        nextSpawnTime = elapsed + wave.spawnInterval;
        if (trySpawn()) SpawnedThisWave++;
        if (SpawnedThisWave >= wave.enemyCount) State = WaveState.WaitingForNextWave;
    }
}
