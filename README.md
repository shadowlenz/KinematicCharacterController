# KinematicCharacterController
My own custom character controller. It uses substeps technique to detect collisions

Features:

- Walking on any gravity direction: Such as walls if you must. Manually adjust the gravity direction.
- Collisions: If character goes super fast. May break if you are going over 100 units per sec but you can always increase sampling in code.
- Slope limits
- Steps: However it is limitated to the radius of your capsule as it casts a sweep on gravity's direction.
- Ground snap: So your character does not bunny hop on slopes. You can manually decrease its distanceSnap ray if you jumped until it finds the ground again.
- Moves on moving/rotating platforms

Notices:

- This cc has an InputMove() method in the motor script that you can disable or delete as it only serves as a demo to start. Otherwise call any moves formula within the Move() method.
- This cc should be treated like Unity's character controller as it does not automatically fall if you walk off the edge. It's up to you to make the velocity. 

https://twitter.com/LenZ_Chu/status/1388902584191180803
