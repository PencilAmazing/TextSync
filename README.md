# TextSync

Sync a file to an application's TextArea. Choose a file, give it a window handle and a TextArea control ID (both as Hex, **without** the "0x" prefix) and it'll send `WM_SETTEXT` events to that specific control every time the file is updated/touched. The file selected will be previewed below.

# POSSIBLE BUGS:
FileWatcher is a bit weird, in that it fires events before a file is actually ready to be read. So we just retry with a one second delay, kinda weird. Visual studio itself doesn't simply modify a file, but makes a copy and replaces the old version with it. I believe notepad does that for big enough files. See https://failingfast.io/a-robust-solution-for-filesystemwatcher-firing-events-multiple-times/

I think we process the same file multiple times, because FileWatcher is weird and doesn't exactly reflect what File Explorer would like you to believe
# FUTURE PLANS
* Make a Spy++ type control picker, there's a stackoverflow link I'm just going to copy because it's good enough
* More error checking, nicer layout, polish in general. This is something I actually need to teach others how to use and it would be nice if it didn't silenty delete all work :/.