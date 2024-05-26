create table extensions (
                            extid INTEGER PRIMARY KEY autoincrement,
                            extension varchar(32) unique,
                            description varchar(128)
);

create table thumbnails (
                            thumbid INTEGER not null PRIMARY KEY AUTOINCREMENT,
                            imgpath varchar(256) not null,
                            isdefault boolean not null,
                            extension INTEGER,
                            FOREIGN KEY (extension) REFERENCES extensions(extid)
);

create table files (
                       fileid varchar(128) not NULL PRIMARY KEY,
                       fpath varchar(256) not null,
                       dname varchar(128) not null,
                       description varchar(512),
                       thumbid INTEGER,
                       extension INTEGER,
                       FOREIGN KEY (thumbid) REFERENCES thumbnails(thumbid),
                       FOREIGN KEY (extension) REFERENCES extensions(extid)
);

create table tags (
                      tagid INTEGER not null PRIMARY KEY AUTOINCREMENT,
                      tagname varchar(256) not null,
                      parent INTEGER,
                      ismeta boolean,
                      FOREIGN KEY (parent) REFERENCES tags(tagid)
);

create table filetags (
                          fileid varchar(128) not NULL,
                          tagid INTEGER not NULL,
                          PRIMARY KEY (fileid, tagid),
                          FOREIGN KEY (fileid) REFERENCES files(fileid),
                          FOREIGN KEY (tagid) REFERENCES tags(tagid)
);

create table tagaliases (
                            tagid INTEGER not NULL,
                            aliasid INTEGER NOT NULL,
                            alias varchar(64) not NULL UNIQUE,
                            primary key (tagid, aliasid),
                            FOREIGN KEY (tagid) REFERENCES tags(tagid)
);



create table classes (
                         classid INTEGER PRIMARY KEY AUTOINCREMENT,
                         name varchar(128) NOT NULL UNIQUE,
                         description varchar(512)
);

create table extclasses (
                            extension INTEGER not NULL,
                            classid INTEGER not NULL,
                            primary key (extension, classid),
                            FOREIGN KEY (extension) REFERENCES extensions(extid),
                            FOREIGN KEY (classid) REFERENCES classes(classid)
);
