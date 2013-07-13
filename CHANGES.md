Changes
=========

13/07/2013

 Finally finished the project I have been working on and can give this some attention!
 1. Now works with proxy objects
 2. Bugfix for setting an associated entity to null (should remove the relation but threw an exception)
 3. Testing changed to use transactions & rollbacks instead of deletes
 4. All tests now pass
 5. Added tests and code for new scenario (cyclic navigational properties)
 6. Renamed project files from EFDetachedUpdate to GraphDiff
 7. Added nuget specifications