# Patchy Release Checklist

To publish an update, the following needs to happen:

1. Increment the version number. Update the preferences with the friendly version and update the hard version in 
   MainWindow.Logic.cs.
2. Commit the changes, create a git tag ("v1.x", etc), and push the new tag to GitHub.
3. Compile Patchy in the RELEASE and PORTABLE configurations.
4. Zip up the portable build. Upload this zip and the RELEASE installer.
5. Prepare a changelog.
6. Create a torrent of the RELEASE build's installer.
7. Upload the changelog to the website, and update the download links and version number.
8. Update http://sircmpwn.github.com/Patchy/update.json

update.json should look like this:

    {
        "FriendlyVersion":"updateme",
        "Version":updateme,
        "Description":"Changelog goes here",
        "MagnetLink":"magnet:?...",
        "HttpLink":"http://example.com",
        "DiffUrl":"http://example.com"
    }

Once update.json is taken care of, users will start to be offered the update.