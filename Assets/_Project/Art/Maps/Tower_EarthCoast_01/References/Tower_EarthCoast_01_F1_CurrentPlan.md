# Tower EarthCoast 01 - F1 Current Plan

- Source scene: `Assets/Scene/AbandonedBuilding_Blockout.unity`
- Generated from saved scene geometry: room `Floor*`, wall `Wall_*`, connector `Run`, and F1 slab/rim plate objects under `Tower_v3_Whitebox`.
- `COLLAPSE` has no saved floor pad, so its structure footprint is inferred from saved wall/rim geometry.
- Size classification: S <= 25 sqm, M <= 80 sqm, L > 80 sqm, based on saved floor footprint.

Room counts: S=6, M=6, L=3, total=15

| Room | Size | X | Z | W | D | Area sqm |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| WAREHOUSE | L | 0.00 | -8.00 | 12.00 | 8.00 | 96.0 |
| POWER | S | 0.00 | 10.00 | 4.00 | 4.00 | 16.0 |
| TEMP | S | 4.00 | 14.00 | 4.00 | 4.00 | 16.0 |
| SECUR | S | 8.00 | 10.00 | 4.00 | 4.00 | 16.0 |
| LOBBY | L | 12.00 | 0.00 | 12.00 | 8.00 | 96.0 |
| SAMPLE | S | 12.00 | 10.00 | 4.00 | 4.00 | 16.0 |
| HALL | L | 12.00 | 16.00 | 12.00 | 8.00 | 96.0 |
| DORM | M | 12.00 | 24.00 | 8.00 | 8.00 | 64.0 |
| CANTEEN | M | 12.00 | 32.00 | 8.00 | 8.00 | 64.0 |
| FOREMAN | M | 21.86 | 35.88 | 7.99 | 7.99 | 63.8 |
| WORKSHOP | M | 24.00 | 8.00 | 8.00 | 8.00 | 64.0 |
| PUMP | S | 26.00 | 0.00 | 4.00 | 4.00 | 16.0 |
| REBAR | M | 34.00 | 8.00 | 8.00 | 8.00 | 64.0 |
| DOCK | M | 34.00 | 16.00 | 8.00 | 8.00 | 64.0 |
| SHANTY | S | 34.00 | 24.00 | 4.00 | 4.00 | 16.0 |

## Special / Structure Nodes

| Node | Type | X | Z | W | D |
| --- | --- | ---: | ---: | ---: | ---: |
| COLLAPSE | STRUCT | 0.00 | 23.86 | 12.14 | 16.14 |
| FIRE | STRUCT | 29.79 | 35.79 | 8.16 | 8.16 |
| STAIRA1 | STRUCT | 26.00 | 26.41 | 4.00 | 11.19 |
| STAIRB1 | STRUCT | 0.00 | 16.00 | 4.00 | 8.00 |
| VAN | STRUCT | 10.24 | -8.00 | 19.80 | 8.00 |

Geometry counts included in SVG: slab/rim plates=2, connector runs=13, wall segments=99.