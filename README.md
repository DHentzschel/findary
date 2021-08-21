# Findary - track all binaries in your repo
In short, this tool is made for finding and tracking binaries in (large) git repos.  
Currently, the function to check whether a file is binary is to check for `\0` (`NUL` byte) occurrence.  
To differ between "real" binaries and encoded files, the following character encodings are detected using its [BOM](https://en.wikipedia.org/wiki/Byte_order_mark):

  - UTF-1
  - UTF-7
  - UTF-8
  - UTF-16BE
  - UTF-16LE
  - UTF-32BE
  - UTF-32LE
  - UTF-EBCDIC
  - BOCU-1
  - SCSU
  - GB 18030

## Usage
Currently, there is no graphical user interface to use this program.  
If not passed, `--directory` is set to the current directory.

#### Command line interface
The following options are available through arguments:

`findary [--directory/-d <path>] [--track/-t] [--recursive/-r] [--measure/-m] [--verbose/-v] [--version] [--help]`


The following help text can be printed *(via `--help`)*:

    findary 1.0.0.0
    Copyright (C) 2021 findary
      -d, --directory      Set directory to process
      -i, --ignoreFiles    Set whether globs from .gitignore should be respected. The root's directory should contain a file .gitignore
      -m, --measure        Set whether measured time should be printed
      -r, --recursive      Set whether directory should be processed recursively
      -s, --stats          Set whether statistics should be printed
      -t, --track          Set whether files should be tracked in LFS automatically
      -v, --verbose        Set whether the output should be verbose
      --help               Display this help screen.
      --version            Display version information.

## General

### Requirements  

- .NET `5` or higher must be installed to run the program _(or download `-full` zip file)_

### Installation
1. Check the latest zip file from the release section  And choose the right zip link e.g.  
  
    `https://github.com/DHentzschel/findary/releases/download/1.0/findary-1.0-dotnet5-windows-amd64.zip`
  
2. Download and extract it and move the folder content to the desired location.  
3. Add the path to `PATH` variable

    - for Linux `$PATH = $PATH:<location>`
    - for Windows _(Powershell)_ `$env:Path += ";<location>" `
 
### Update
Do Step 1 and 2, it is only required once to add the path to the `PATH` variable
