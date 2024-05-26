BEGIN;
INSERT INTO extensions (
    extension, description
) VALUES (
             'png', 'Portable Network Graphics Image Format'
         );
INSERT INTO tags (
    tagname, parent, ismeta
) VALUES (
             'photos', NULL, false
         );

INSERT INTO tags (
    tagname, parent, ismeta
) VALUES (
             'pets', NULL, false
         );

INSERT INTO tags (
    tagname, parent, ismeta
) VALUES (
             'cute', (SELECT t.tagid FROM tags t WHERE t.tagname = 'pets'), false
         );

INSERT INTO tags (
    tagname, parent, ismeta
) VALUES (
             'luna', NULL, false
         );


INSERT INTO tagaliases (
    tagid, alias, aliasid
) VALUES (
             (SELECT t.tagid FROM tags t WHERE t.tagname = 'photos'),
             'pictures',
             1
         );

INSERT INTO tagaliases (
    tagid, alias, aliasid
) VALUES (
             (SELECT t.tagid FROM tags t WHERE t.tagname = 'pets'),
             'critters',
             2
         );

INSERT INTO tagaliases (
    tagid, alias, aliasid
) VALUES (
             (SELECT t.tagid FROM tags t WHERE t.tagname = 'luna'),
             'little_lunita',
             3
         );

INSERT INTO thumbnails (
    imgpath, isdefault, extension
) VALUES (
             '/.thumbs/png-default.jpg',
             TRUE,
             (SELECT e.extid FROM extensions e WHERE e.extension = 'png')
         );


INSERT INTO files (
    fileid, fpath, dname, description, thumbid, extension
) VALUES (
             'c093c0eb-d62a-493f-ac2b-94a06ceb2238',
             '/home/skye/pics/cat.png',
             'Luna and Fireworks',
             'A photo of Luna on new year''s 2024, admiring the fireworks.',
             (SELECT t.thumbid
              FROM thumbnails t
                       JOIN extensions e ON e.extid = t.extension
              WHERE e.extension = 'png'),
             (SELECT e.extid
              FROM extensions e
              WHERE e.extension = 'png')
         );

INSERT INTO classes (
    name, description
) VALUES (
             'Images',
             'Graphical files representing pixel or vector still images.'
         );
INSERT INTO extclasses (
    extension, classid
) VALUES (
             (SELECT e.extid
              FROM extensions e
              WHERE e.extension = 'png'),
             (SELECT c.classid
              FROM classes c
              WHERE c.name = 'Images')
         );

INSERT INTO extensions (
    extension, description
) VALUES (
             'jpg', 'Joint Photographic Experts Group image file'
         );
INSERT INTO extensions (
    extension, description
) VALUES (
             'jpeg', 'Joint Photographic Experts Group image file (alt. file name)'
         );

INSERT INTO extclasses (
    extension, classid
) VALUES (
             (SELECT e.extid
              FROM extensions e
              WHERE e.extension = 'jpg'),
             (SELECT c.classid
              FROM classes c
              WHERE c.name = 'Images')
         );
INSERT INTO extclasses (
    extension, classid
) VALUES (
             (SELECT e.extid
              FROM extensions e
              WHERE e.extension = 'jpeg'),
             (SELECT c.classid
              FROM classes c
              WHERE c.name = 'Images')
         );

INSERT INTO filetags (
    fileid, tagid
) VALUES (
             'c093c0eb-d62a-493f-ac2b-94a06ceb2238', (SELECT tagid FROM tags WHERE tagname = 'photos')
         );

INSERT INTO filetags (
    fileid, tagid
) VALUES (
             'c093c0eb-d62a-493f-ac2b-94a06ceb2238', (SELECT tagid FROM tags WHERE tagname = 'luna')
         );

INSERT INTO files (
    fileid, fpath, dname, description, thumbid, extension
) VALUES (
             '8bb40d93-f3a3-48ff-afa9-faf7d7e45e8f',
             '/home/skye/pics/mountain.png',
             'Cool mountain photo',
             'Big rock go brrr.',
             (SELECT t.thumbid
              FROM thumbnails t
                       JOIN extensions e ON e.extid = t.extension
              WHERE e.extension = 'png'),
             (SELECT e.extid
              FROM extensions e
              WHERE e.extension = 'png')
         );

INSERT INTO filetags (
    fileid, tagid
) VALUES (
             '8bb40d93-f3a3-48ff-afa9-faf7d7e45e8f', (SELECT tagid FROM tags WHERE tagname = 'photos')
         );
COMMIT;