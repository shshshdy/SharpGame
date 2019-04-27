REM Make the directory batch file resides in as the working directory.
pushd %~dp0

glslangvalidator -V Textured.vert -o Textured.vert.spv
glslangvalidator -V Textured.frag -o Textured.frag.spv

popd

pause