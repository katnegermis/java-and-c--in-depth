File system
=============

Structure
-----------
We decided to take a lot of inspiration from the FAT file system (http://en.wikipedia.org/wiki/File_Allocation_Table), also naming our file system JCDFAT.

The smallest unit of allocation in JCDFAT is one block, which currently is 2^12 bytes (4KB).

The first block of the file system contains meta data (description below). Even though the meta data currently only is 28 bytes, it has a full block allocated to it, as described above.

The next block(s) contain(s) the File Allocation Table (FAT). Depending on how large the file system is, the FAT will span one or more blocks. Since we're using 32 bit integers, and blocks of size 4KB, each FAT block allows us to address 4MB.  This means that the smallest JCDFAT file system allowed is 4MB. It also means that the largest JCDFAT file system possible theoretically is 16 TB, but due to an implementation detail, it currently is 2 TB. The size of the FAT is _included_ in the size of the file system. The FAT takes up around 1/1024th of the file system; this means that if the file system is 16 TB, the FAT will be 16 GB.
There is no bound on the size of individual files, except the size of the file system.

The two blocks following the FAT are reserved for the root directory and the search file. The root directory is the root folder of the file system. The search file is the file in which we wish to store meta data for indexing the file system, allowing us to implement file search in milestone 2 of the project.

~~~~~
                File system structure
          (`|` represents a block boundary)

| Meta data | FAT block(s) | Root directory | Search file |
| data | data | data | data | data | data | data |  data  |
                        . . .
| data | data | data | data | data | data | data |  data  |
~~~~~

~~~~~
     Meta data
                          Size
+-----------------------+
| Magic number          | 4B
+-----------------------+
| Block size in         | 4B
| power-of-2 bytes      |
+-----------------------+
| Number of FAT blocks  | 4B
+-----------------------+
| Free blocks           | 4B
| (without expanding)   |
+-----------------------+
| First free block      | 4B
+-----------------------+
| Root directory block  | 4B
+-----------------------+
| Search file block     | 4B
+-----------------------+

Total size: 28 B
~~~~~

We use the structure called JCDDirEntry (described below) to represent files on disk. Each directory entry is 256KB, meaning that we can have 2^12/2^8 = 16 entries in each block. Since the smallest unit of allocation is 4 KB, a folder takes up at least 4 KB of space. More generally, a folder takes up $\text{ceil}(text{entries} / 16) * 2^12$ bytes.

As can be seen on the figure below, the maximum length of a file name is 240 bytes. The string is interpreted as unicode, though, which means that the length of the file name is at most 120 characters.


~~~~~
JCDDirEntry
                  Size
+---------------+
| Name          | 240B
+---------------+
| Size          | 8B
+---------------+
| isFolder      | 4B
+---------------+
| First block   | 4B
+---------------+

Total size: 256B
~~~~~

File system events
--------------------

The following is an explanation of how the implementation of our file system behaves in different scenarios.

In the following we assume that there is always enough free space, and that the source and destination files and folders exist/don't exist as required.


Finding a specific folder or file
---------------------------
- Starting at the root folder, recursively identify the directory entry of the next folder specified by the given path.
- Once the parent folder of the specified file or folder has been found, find the specified file or folder.


Allocating space for a file
-----------------------------
- Figure out how many blocks the file requires, $\text{ceil}(\frac{\text{size in bytes}}{2^12})$.
- Starting from the first free block, walk the FAT in increments of 1, chaining free blocks.
- Mark the last used block as 'end of chain'.


Creating a file or folder
---------------------------
- Allocate enough blocks to store the file. This is done by finding free entries in the FAT and chaining them.
- Store the file in the newly allocated blocks.
- Write a directory entry in the destination folder.


Deleting a file (or folder)
-----------------------------
- (Loop over all files/folders and perform delete file.)
- Starting from the first block of the file, walk the FAT chain and delete all entries.
- Delete the directory entry from the parent folder.


Moving a file or folder
---------------
- Move the file entry from the source folder to the destination folder. File contents are not actually moved.


Renaming a file or folder
-----------------
- Rewrite the name in the file's directory entry.


Bonus features
----------------
- Basic
    1. Compression implemented with third-party library. (1 point)
    2. Encryption implemented with third-party library. (1 point)
    3. Elastic disk: the virtual disk can dynamically grow or shrink, depending on its occupied space. (2 points)
        - Our virtual disk initially only takes up the space required by meta data, and hereafter expands itself when importing files. It does not, however, shrink when files are deleted.
- Advanced
    1. Compression implemented from scratch (you may use arithmetic compression 9 ). (2 points)
    2. Encryption implemented from scratch. (3 points)
    3. Large data: the VFS core can store and operate with an amount of data that doesn’t a standard PC RAM (about 4 Gb). (3 points)
        - Check. Implemented by using buffers when reading/writing files.


Features that would be nice to have, but haven't been implemented:
- (Don't decrease size of vfs when deleting)
- Don't decrease size of folder when the number of entries in it decreases below a multiple of 16.

