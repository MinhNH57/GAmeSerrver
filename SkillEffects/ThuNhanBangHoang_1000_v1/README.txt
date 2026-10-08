THU NHAN BANG HOANG - SKILL 16001 / BULLET RES 1000

Generated source assets:
- Source/AnimFile2_SpriteSheet_4x4.png: flying sword-energy animation, 16 frames.
- Source/AnimFile4_SpriteSheet_4x4.png: impact animation, 16 frames.

Individual frames:
- Frames/AnimFile2/AnimFile2_00.png ... AnimFile2_15.png (384x256 each).
- Frames/AnimFile4/AnimFile4_00.png ... AnimFile4_15.png (362x272 each).

Suggested BulletActionSet values:
ID=1000
BundleDir=Resources/Skill/1000
HasFlyAction=1
HasFadeOutAction=0
HasExplodeAction=1
Use16Dir=0
AutoRotate=1
UseTrailEffect=1
TrailDuration=0.2
TrailPeriod=0.05

Unity import:
1. Import each sprite sheet as Sprite (2D and UI), Sprite Mode Multiple.
2. Slice by Grid By Cell Count: 4 columns x 4 rows.
3. Keep frame order left-to-right, then top-to-bottom.
4. Build the flight sheet as AnimFile2.unity3d and impact sheet as AnimFile4.unity3d.
5. Build separately for Windows and Android into Data/Resources/Skill/1000.
