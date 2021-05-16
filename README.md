# LenZ's KinematicCharacterController
My own Unity custom character controller. It uses substeps technique to detect collisions.

 <br/> <br/>
Features:

- **Walking on any gravity direction** - Such as walls if you must. Manually adjust the gravity direction.
- **Collisions** - If character goes super fast. May break if you are going over 100 units per sec but you can always increase sampling in code.
- **Slope limits** - Set it to 0 to ignore.
- **Steps** - However it is limitated to the radius of your capsule as it casts a sweep on gravity's direction.
- **Ground snap** - So your character does not bunny hop on slopes. You can manually decrease its distanceSnap ray if you jumped until it finds the ground again.
- **Moves on moving/rotating platforms**

 <br/> <br/>
Notices:
- This cc has an InputMove() method in the motor script that you can disable or delete as it only serves as a demo to start. Otherwise call any moves formula within the Move() method.
- This cc should be treated like Unity's character controller as it does not automatically fall if you walk off the edge. It's up to you to make the velocity. 
- Because this controller uses substeps to guarantee collision hits and not phasing thru. Going super fast may not interact with other rigidbodies correctly. It's a trade off since if this controller uses rb.MovePosition(), it would not properly collide with wall obsticles, goes thru hill slopes on high speed, and/or vibrate to reposition itself again because it gets called per FixedUpdate.

<br/> <br/>
Preview:
https://twitter.com/LenZ_Chu/status/1388902584191180803

<br/> <br/>
Limitations:
- If you have slope limits on while bumping into 2 seperate slope gameobjs, you be allowed to travel upwards. Due to the bumping back and forth. Advise to have them be the same mesh.
- If you make a boxed shaped slope, the slope detection will not work if your running on it's edge due to CapsulCast or SphereCast hitting the edge returns an interpoated normals. Further readings: https://forum.unity.com/threads/formula-used-by-spherecast-to-iterpolate-normals-when-hitting-an-edge.326217/

<br/> <br/>
Final Thoughts:
 - I rlly wanna impose that it is 'kinematic' only because it holds a rigidbody to interact with physics obj. Because this controller moves with many samplings per frame, it doesn't interpolates for physics and may brush off physic objs or phase thru them if you move too fast or cornering them. it's like using transform.position or rb.position multiple times to check for collisions. So I do not reccommend to use physics as a gameplay element using this controller other than astetics. 
 - To make the controller interact with rigidbodies, set their layers other than the obsticle layers.
 - You can also set true to 'use kinematic' on the rigidbody for forces to push against you.
 - As I'm making this, I discovered this wizard here points out the limitations of what my controller has and why Unity needs better tools to detect colliders.  Here's some a link for further readings. https://forum.unity.com/threads/dotsphysics-features-i-wish-i-had-in-monobehaviour-physics-physx.1057004/
<br/> <br/> You may use this freely and hopefully it be useful  :3
