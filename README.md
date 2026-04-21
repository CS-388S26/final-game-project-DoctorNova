# Space Shooter

The space shooter is a mobile game in which the player has to destroy all enemy spaceships before he gets destroyed by them.

## Controls

- Tilt phone: Rotate ship (pitch/roll)
- Swipe up/down: Increase/Decrease speed
- Tab: Shoot normal projectile
- Long/strong press: Shoot powerful projectile

## Interaction techniques

The previous version of this game used the gyroscope and touch/swiping. In the new version for the final project, I kept the touch/swipe and replaced the gyroscope with the accelerometer which makes the control a lot easier for the player. I also added the pressure sensor to fire a more powerful projectile. If a pressure sensor is not supported by the device, then pressing longer will also fire a more powerful projectile.

## Rendering optimization techniques

In addition to the instance render from the previous version, I added an atlas for the HUD and the Menus to combine the images.

## Research topic: Procedural terrain generation

In Level 2 the player flies over an alien planet and the terrain below him is procedurally generated while he flies. I am using the Perlin Noise function that unity provides to generate pseudo random values for the height of the terrain. To add more detail, I combine 4 different Perlin Noise values that are modified by a different amplitude and frequency. All parameters of the MeshGenerator script can be modified to change the terrain. The only restriction is that the Gameobject must be placed at the origin (0, 0,0) because the vertices are generated in world coordinates and they don’t consider the Gameobjects position.

The biggest challenge was not generating the terrain but integrating it into the game. Regenerating the mesh on each frame so that it moves with the player was impossible given the performance restrictions of a mobile phone. To get around this I am only adding vertices to the mesh in the direction in which the player is moving while removing the vertices behind him.

Adding a mesh collider to the game object was also terrible for performance because of the constant updating of the mesh. To improve performance, I do not use unity’s physics system to check for collisions. The AI checks with the closest vertices around it and avoids them as part of the BOID movement. To prevent the player from flying through the terrain I added a custom function to the Terrain generator that checks for a collision with the terrain by finding the closest vertex of the terrain and does a sphere vs triangle collision check with that part of the terrain. This custom collision handling doubled the games FPS.
