# Connections Probe sweep — Editor previews

These deterministic Unity 6000.3.11f1 Editor captures show the authored Probe graphic in both Connections scenes at 1920×1080. The visual witness drives the cosmetic clock directly. `active` and `active-later` are two sweep positions; `expiration` shows the rectangle halfway through its fade after the host has already cleared classification. The Connections crowded capture uses five long-running ordinary connections, one hostile, on the selected edge. These are Editor renders, not interactive Play Mode screenshots.

| Scene | Captures |
| --- | --- |
| Connections | [Inactive](Connections-inactive-1920x1080.png) · [Activation](Connections-activation-1920x1080.png) · [Active](Connections-active-1920x1080.png) · [Later sweep](Connections-active-later-1920x1080.png) · [Diagonal](Connections-diagonal-E2-1920x1080.png) · [Crowded](Connections-crowded-1920x1080.png) · [Expiration](Connections-expiration-1920x1080.png) |
| Connections Advanced | [Inactive](ConnectionsAdvanced-inactive-1920x1080.png) · [Activation](ConnectionsAdvanced-activation-1920x1080.png) · [Active](ConnectionsAdvanced-active-1920x1080.png) · [Later sweep](ConnectionsAdvanced-active-later-1920x1080.png) · [Diagonal](ConnectionsAdvanced-diagonal-E2-1920x1080.png) · [Expiration](ConnectionsAdvanced-expiration-1920x1080.png) |

The witness checks local sweep direction and 80% scanner length on every authored link in both scenes. It also exercises synthetic horizontal, diagonal, and vertical rotations; the authored scenes have vertical and diagonal links but no horizontal link. Manual Play Mode review should check the moving band at 1280×720 and 1920×1080, especially on crowded links, and adjust the serialized opacity/width if the sweep competes with path lines or pulses.
