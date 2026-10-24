# eAkte

eAkte is my second big project at **DreiF in Berlin**, and I built it from scratch. It is a complete, multi-tenant electronic file management system: users create Akten and Vorgänge, organise documents in folders, keep track of contacts and changes, and work on Office documents directly in the browser.

I built it with **ASP.NET Core**, **C#** and **SQL Server**. The front end is plain HTML, CSS and JavaScript.

![eAkte overview 1](docs/screenshots/1.png)

![eAkte overview 2](docs/screenshots/2.png)

![eAkte overview 3](docs/screenshots/3.png)

## What it does

### One system, many companies
The system is multi-tenant (in German: *mandantenfähig*). Several clients, called **Mandanten**, work in the same installation, and their data is strictly separated. Every query is limited to the active Mandant, so nobody can see another company's files.

### Akten and Vorgänge
The heart of eAkte is creating and managing **Akten** (files) and **Vorgänge** (cases). An Akte is created inside a category and group, and each Akte can hold any number of Vorgänge that keep related work, documents and information together. Inside a Vorgang, everything is organised with folders and subfolders.

### A deep file structure
Documents don't end up in one big pile. They live in a hierarchy that mirrors how files are organised in real offices:

```
Mandant
 └── Kategorie
      └── Hauptgruppe
           └── Nebengruppe
                └── Akte
                     └── Vorgang
                          └── Ordner
                               └── Unterordner ...
                                    └── Dokument
```

Folders can be nested as deeply as needed, and every document sits in exactly the right place in this structure.

### Change history
Changes are recorded, so it is always possible to see what happened to a file or case, who changed it and when.

### Contacts
Contacts can be stored and linked to the files and cases they belong to, so the people involved are always at hand.

### Import, export and PDF
Documents can be **imported** into any folder and **exported** for use outside the system. A **built-in PDF converter** turns finished documents into PDF directly in the application, which is handy for archiving, sharing and printing.

### Editing Office documents in the browser
Microsoft Office documents can be opened straight from eAkte in Word, edited there, and saved back into the system through **WebDAV**, with no manual download and upload. It works well with Microsoft Word 2019.

### Check-out and check-in
To make sure two people don't overwrite each other's work, a document is **checked out** (locked) while someone is editing it and **checked in** again when they are done. The system stores who locked the document and since when.

### Roles and permissions
Not every user should be allowed to do everything. A flexible **role system** controls who can do what: viewing, creating, editing, deleting, importing, exporting and converting can each be granted separately. Access can also be restricted **per object**, such as an Akte, a Vorgang, a folder or a document, and roles can be combined. All checks are enforced on the server, not just hidden in the interface, and they work together with the Mandant separation.

### Single sign-on (in planning)
The plan is to connect eAkte to a central login service via **OpenID Connect**, so that several applications, including a .NET Framework 4.8 app, can share one sign-in. Failed sign-ins show only a generic message, so no internal details are revealed.

## Technologies

| Area | What I used |
|------|-------------|
| Backend | ASP.NET Core, C# |
| Database | Microsoft SQL Server |
| Documents | WebDAV, Microsoft Office integration, PDF conversion |
| Authentication | OpenID Connect (OpenIddict) |
| Front end | HTML, CSS, JavaScript |
| IDE | Visual Studio |

## Security

Secrets and connection strings are not part of this repository. All data access is scoped by Mandant and protected by role-based permissions, and documents are locked while they are being edited.

## About the project

After AdminPanel, eAkte was the bigger challenge: a full system with many parts that have to work together, from the file hierarchy, permissions and change history to locking and the Office integration. I designed and built it from the ground up.
