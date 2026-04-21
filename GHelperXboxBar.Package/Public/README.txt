This folder is referenced by the Xbox Game Bar widget AppExtension's
PublicFolder="Public" attribute in Package.appxmanifest. It must exist in the
produced MSIX or Windows will drop the AppExtension registration (the widget
will never appear in the Game Bar widget menu). Leave this .gitkeep in place
so the folder survives git and is copied into the package.
