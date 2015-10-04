massUpdateFromGit
=================
    
Application allows you to update several local Git repositories from the main remote one.

![massUpdateFromGit main window screenshot](/img/mainwindow.png?raw=true "massUpdateFromGit main window screenshot")

You just need to set the link to the main Git repository, the path to local repository, the desired branch and a list of local repositories to update.

Settings
========

You can set some settings in the `.config` file.

#### dirOfScenaries

Default path for saving scenaries.

3rd party
=========

The application is written in `C#`, `WPF` with `Visual Studio 2013` (if that matters) and `.NET 4.5.1`. Those nasty bastards did all the work, I just wrote a few lines of code.

Also I used [Ookii.Dialogs](http://www.ookii.org/Software/Dialogs/) for open file/folder dialogs, because for some reasons there is no such thing as OpenDirectoryDialog in `WPF`.

And I snatched some (all) icons from [Iconfinder](https://www.iconfinder.com/).