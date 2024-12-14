**Nex-Gen Dragon Framework**
========================

## 1. Goal
---

### Main Target 
- Let developers focus on **GamePlay**

### Key word
- Lightweight
- Modularization
- Scalability

## 2. Architecture & Components
---

### Common
- core
- utility

### Feature
- asset
- network
- sound

### Pattern
- mvc
- scene

### Chart

```
+-----------------------------------------------------------------+
|                                                                 |
|  Nex-Gen Dragon                                                 |
|                                                                 |
|                                                                 |
|   +-----------------------------------+                         |
|   |                                   |                         |
|   |  +--------+        +-----------+  |                         |
|   |  |        |        |           |  |                         |
|   |  |  Core  <-------->  Utility  |  |                         |
|   |  |        |        |           |  |                         |
|   |  +--------+        +-----------+  |                         |
|   |                                   |                         |
|   +---------------^-------------------+                         |
|                   |                                             |
|                   |                                             |
|   +---------------+----------------------------------------+    |
|   |                                                        |    |
|   |  +-----------+     +-------------+      +-----------+  |    |
|   |  |           |     |             |      |           |  |    |
|   |  |   Asset   <----->   Network   |      |   Sound   |  |    |
|   |  |           |     |             |      |           |  |    |
|   |  +-----------+     +-------------+      +-----------+  |    |
|   |                                                        |    |
|   +---------------^----------------------------------------+    |
|                   |                                             |
|                   |                                             |
|   +---------------+-----------------+                           |
|   |                                 |                           |
|   |  +---------+       +-------+    |                           |
|   |  |         |       |       |    |                           |
|   |  |  Scene  |       |  Mvc  |    |                           |
|   |  |         |       |       |    |                           |
|   |  +---------+       +-------+    |                           |
|   |                                 |                           |
|   +---------------------------------+                           |
|                                                                 |
+-----------------------------------------------------------------+
```

## 3. Rules & Principles
---

### Principles
- Keep it simple and stupid.
- Don't repeat yourself.
- Don't think the requirement that not exist right now.

### Rules
- IObject is basic interface, NexgenObject and NexgenBehavior inherit from it. Everything in this framework must inherit from NexgenObject or NexgenBehavior.
- If you can, use the interface in Core/Object folders.
    - Top level manager must implement IManager.
    - If you want to register a Updater to GameFacade, you must implement IUpdater, IGapUpdater or ISecondUpdater.
    - If something could show in scenes, it must implement IDisplayable.
    - If something need load some resources before use it , it must implement ILoadable or IAsyncLoadable.
- StateMachine doesn't implement ITick interface. Because it must be managed by it's owner.

## 4. Component Design
---

### Network protocol

- Client to Server

```
+----------------------------------------------------------------------------+
|                                   header                                   |
+---------------+-------------------+--------+-------+------------+----------+
|               |                   |        |       |            |          |
|   magic code  |   client version  |   os   |  type |  zip type  |  length  |
|               |                   |        |       |            |          |
+---------------+-------------------+--------+-------+------------+----------+
|                                    body                                    |
+-----------------------+--------------------------+-------------------------+
|                       |                          |                         |
|     config version    |      asset version       |       ui version        |
|                       |                          |                         |
+----------------------------------------------------------------------------+
|                       |                          |                         |
|      command id       |        action id         |      return code        |
|                       |                          |                         |
+----------------+------+-----------+--------------+----+--------------------+
|                |                  |                   |                    |
|    user id     |     seq num      |     timestamp     |      session       |
|                |                  |                   |                    |
+----------------+------------------+-------------------+--------------------+
|                                                                            |
|                                                                            |
|                                 logic data                                 |
|                                                                            |
|                                                                            |
+----------------------------------------------------------------------------+
```

- Server to Client

```
+----------------------------------------------------------------------------+
|                                   header                                   |
+---------------+------------------------------------+------------+----------+
|               |                                    |            |          |
|   magic code  |                type                |  zip type  |  length  |
|               |                                    |            |          |
+---------------+------------------------------------+------------+----------+
|                                    body                                    |
+-----------------------+--------------------------+-------------------------+
|                       |                          |                         |
|      command id       |        action id         |      return code        |
|                       |                          |                         |
+-----------------------+-----------+--------------+-------------------------+
|                                   |                                        |
|            seq num                |                timestamp               |
|                                   |                                        |
+-----------------------------------+----------------------------------------+
|                                                                            |
|                                                                            |
|                                 logic data                                 |
|                                                                            |
|                                                                            |
+----------------------------------------------------------------------------+
```
