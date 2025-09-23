--
-- Table structure for table `custom_resources`
--
DROP TABLE IF EXISTS `custom_resources`;
CREATE TABLE `custom_resources` (
  `id` INTEGER NOT NULL PRIMARY KEY,
  `file_name` TEXT NOT NULL,
  `uploaded_by` TEXT NULL
);