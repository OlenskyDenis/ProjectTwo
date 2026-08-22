# Contract: IChunkStreamingPipeline

**Component**: `ProjectTwo.Terrain.Presentation.Components.TerrainGenerator`

## Overview
Defines the presentation streaming contract, background task delegation, and time-budgeted main-thread chunk ingestion.

## Asynchronous Streaming Protocol

```csharp
namespace ProjectTwo.Terrain.Presentation.Components
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using ProjectTwo.Terrain.Core.Models;

    public interface IChunkStreamingPipeline
    {
        /// <summary>
        /// Initiates asynchronous calculation of heightmap, visual mesh, collision mesh, and river mesh off the Main Thread.
        /// </summary>
        void RequestChunkGeneration(ChunkCoordinate coord, int targetLod, CancellationToken token);

        /// <summary>
        /// Time-budgeted ingestion loop executed each frame on the Main Thread (Max 2.0ms / Max 2 chunks).
        /// </summary>
        void ProcessCompletedChunks();

        /// <summary>
        /// Cancels all active in-flight worker tasks and purges completion queues.
        /// </summary>
        void CancelInFlightTasks();
    }
}
```
