--
-- Table structure for table `settings`
--
DROP TABLE IF EXISTS `settings`;
CREATE TABLE `settings` (
  `id` VARCHAR(50) NOT NULL PRIMARY KEY,
  `value` VARCHAR(100) NULL
);