# Unity Simple Physics
### Lightweight physics systems for Unity

This project provides a collection of physics-based behaviors for Unity, including ropes, cloth, and soft bodies.  
The focus is on **simplicity and usability**, aiming for **minimal authoring effort** while keeping the systems **easy to understand, modify, and integrate** into existing projects.




## 🪢 Rope
**Using Rigidbodies and ConfigurableJoints**

The rope system is split into a generator and a runtime component.  
The generator instantiates a chain of prefab `Rigidbody` points and connects them using `ConfigurableJoint`s, with configurable spacing and joint parameters.

The runtime `Rope` component handles visualization by collecting the generated segment transforms and updating a `LineRenderer` each frame so the rope follows the simulated rigidbody positions.

▶ **Demo:** https://www.youtube.com/shorts/QTXzCxIQ-xY



## 🧵 Cloth
**Using Rigidbodies and ConfigurableJoints**

The cloth system also consists of a generator and a runtime component.  
The generator creates a 2D grid of prefab `Rigidbody` points and connects neighboring points using `ConfigurableJoint`s to form the cloth structure.

The `Cloth` component is responsible for visualization and optional collision behavior.  
It can render the cloth as a wireframe using line renderers or as a dynamically generated mesh whose vertices are updated each frame from the simulated point positions. Optional mesh collider updates prevent rigidbodies from passing through the cloth, provided the cloth points are not spaced too far apart.

▶ **Demo:** https://www.youtube.com/shorts/QTXzCxIQ-xY

![Cloth Image](Images/Cloth.png)



## 🧽 SoftMeshLight
**Lightweight, impulse-based static mesh deformation**

SoftMeshLight is a standalone runtime component and does not use a generator.  
It clones the assigned mesh at runtime and listens for collision events. On impact, nearby vertices are displaced along the collision normal using a smooth falloff within a configurable radius.

The deformation is computed asynchronously to avoid blocking the main thread.  
The mesh collider can optionally be updated so that visual deformation and collision geometry remain consistent.

▶ **Demo:** https://www.youtube.com/shorts/hCs_VBjFWeg

![SoftMeshLight Image](Images/SoftMeshLight.png)



## 🧽➕ SoftMesh (Coming Soon)
**Elastic and dynamic soft-body deformation**

SoftMesh is an improved version of SoftMeshLight and aims to support non-static, elastic deformations.  
Unlike SoftMeshLight, which applies permanent dents, SoftMesh is intended to allow meshes to recover over time and respond dynamically, enabling jelly-like behavior.



## 🧠 SoftBody (Work in Progress)
**Joint-based volumetric soft body using mesh topology**

SoftBody is an experimental soft-body implementation that derives its simulation structure directly from a mesh.  
At runtime, the mesh is cloned and its vertices are merged to remove duplicates. For each unique vertex, a prefab `Rigidbody` is instantiated and positioned at the corresponding vertex location.

Edges are extracted from the mesh triangles, and `ConfigurableJoints` are created between rigidbodies that share an edge. These joints use linear and angular drives to approximate elastic behavior based on the original mesh topology.

Each frame, the mesh is deformed by updating its vertices from the current rigidbody positions.  
An optional mesh collider can be generated and updated dynamically, and collision impulses can be relayed back to nearby rigidbodies to improve interaction with external objects.

This system is still a work in progress and primarily serves as a foundation for more robust volumetric soft-body behavior.

![SoftBody Image](Images/SoftBody.png)
