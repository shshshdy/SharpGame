REM Make the directory batch file resides in as the working directory.
pushd %~dp0

glslangvalidator -V Test.vert -o Test.vert.spv
glslangvalidator -V Test.frag -o Test.frag.spv

popd
