# Findary
## track all binaries in your repo
In short, this tool is made for finding and tracking binaries in (large) git repos.  
Currently, the function to check whether a file is binary is to check for `\0` (`NUL` byte).  
To differ between "real" binaries and encoded files, the following character encodings are detected by its BOM:

  - UTF-1
  - UTF-7
  - UTF-8
  - UTF-16Be
  - UTF-16Le
  - UTF-32Be
  - UTF-32Le
  - UTF-EBCDIC
  - BOCU-1
  - SCSU
  - GB 18030
  

### Requirements  

- .NET `5` or higher must be installed to run the program  

### Installation
1. Check the latest zip file from the release section  And choose the right zip link e.g.  
  - `https://github.com/DHentzschel/findary/releases/download/<version>/findary-<version>-windows-amd64.zip`
  - `https://github.com/DHentzschel/findary/releases/download/<version>/findary-<version>-windows-386.zip`
  - `https://github.com/DHentzschel/findary/releases/download/<version>/findary-<version>-linux-386.zip`
  - `https://github.com/DHentzschel/findary/releases/download/<version>/findary-<version>-linux-amd64.zip`
2. Download and extract it and move the folder content to the desired location.  
3. Add the path to `PATH` variable

    - for Linux `$PATH = $PATH:<location>`
    - for Windows _(Powershell)_ `$env:Path += ";<location>" `
 
### Update
Do Step 1 and 2, it is only required once to add the path to the `PATH` variable

## Usage
`findary [-directory/-d <path>] [--track/-t] [--recursive/-r] [--measure/-m] [--verbose/-v] [--version] [--help]`

*Help Text*

    findary <version>
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

