# PatchYmlDowngrader
 Converts between new/old RPCS3 patch format and/or downgrades strings
# Commandline Usage
- arg 1: the path to your patch.yml  
- arg 2: ``-new`` to output in new format, ``-old`` for old format  
- arg 3: ``-str`` (optional) to downgrade strings  
Output should appear next to ``.exe``.
# Known Bugs
- Offset doesn't increment for replaced strings yet (idk why)