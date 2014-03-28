       JCDFS
+-----------------------+
| meta | FAT | root dir |
+-----------------------+
|  data data data data  |
+-----------------------+
|  data data data data  |
+-----------------------+

Total size: (1000 + 1) * k + 1


Meta data
===========               Size
+-----------------------+
| Magic number          | 32 bits
+-----------------------+
| Total size in blocks  | 32 bits
+-----------------------+
| Free space in blocks  | 32 bits
+-----------------------+
| Root directory block  | 32 bits
+-----------------------+
| Search file block     | 32 bits
+-----------------------+
| first free block      | 32 bits
+-----------------------+

Total size: 64B (We make it 1 block to make future calculations easier.)


File
======            Size
+---------------+
| Name          | 256B - the rest
+---------------+
| Size          | 44 bits
+---------------+
| isFolder      | 1 bit
+---------------+
| First block   | 32 bits
+---------------+

Total size: 256B


Math
======
vfs size = x
block size = 2^12
num blocks = 2^x / 2^12
fat size = (num blocks) * 4
