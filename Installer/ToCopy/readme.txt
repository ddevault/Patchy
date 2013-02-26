This directory contains all the assemblies to be embedded as links. This way, every time you compile,
the files are updated with all the dependencies before compiling the installer. In the post-build, 
some of these are compressed and copied into the binary.