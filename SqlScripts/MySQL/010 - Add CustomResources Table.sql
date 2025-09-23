--
-- Table structure for table `custom_resources`
--
DROP TABLE IF EXISTS `custom_resources`;
CREATE TABLE `custom_resources` (
  `id` BIGINT NOT NULL PRIMARY KEY AUTO_INCREMENT,
  `file_name` VARCHAR(100) NOT NULL,
  `uploaded_by` VARCHAR(100) NULL
);