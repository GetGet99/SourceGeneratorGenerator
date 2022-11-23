# SourceGeneratorGenerator
Source Generator Generator will generate code according to Source Generator so you do not need to restart VS every time because you are not editing Source Generator Generator but you are editing Source Generator...

Only intended for personal use but you can use it if you want to.

Credit: I get the idea of how to compile the code from https://github.com/hermanussen/CompileTimeMethodExecutionGenerator

Known Bugs:
* If you reference something it might not be registered yet, it might give you an error, you need to add new MetadataReference yourself in the repo
* Put everything you want to be included in the generator within 1 file, including the attribute you will be using. Yes, multiple classes within 1 file.
* Note: Bugs will not generally be fixed
