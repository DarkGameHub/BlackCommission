# AccidentSquad

AccidentSquad is a 1-4 player co-op commission-running game about a broke office that survives by taking increasingly strange outsourced jobs.

Current MVP direction:

1. Start a solo host or create/join a host room.
2. Spawn in a rundown office.
3. Use the office computer to accept a job.
4. Enter the school mission, find the missing homework notebook, avoid the school anomaly, and return to the exit.
5. Return to the office, claim money/reputation/experience, then spend money on equipment, recovery items, office upgrades, or future agency acquisitions.

See [docs/mvp-core-loop.md](docs/mvp-core-loop.md) for the full MVP design, story background, agent team setup, and first-phase implementation plan.

Current art direction is locked in [docs/art/accidentsquad-style-lock-v1.md](docs/art/accidentsquad-style-lock-v1.md).

Unity setup:

1. Run `Tools > Accident Squad > Setup All (Run This First!)` if the base project has not been generated on this checkout.
2. Run `Tools > Accident Squad > MVP > Setup School MVP`.
3. Run `Tools > Accident Squad > MVP > Validate School MVP`.
4. Open `HQ`, press Play, click `Start Host`, then use the office computer to enter the school mission.

Generated art workflow:

1. On Windows with Blender installed, run `blender --background --factory-startup --python D:/AccidentSquad/docs/art/blender_outsourced_civic_commercial_v4.py`.
2. In Unity, run `Tools > Accident Squad > Art > Import Generated Blender Kit`.
3. The imported prefabs are generated under `Assets/_Project/Prefabs/Art`.
