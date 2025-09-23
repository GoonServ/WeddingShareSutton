--
-- Table structure for table `gallery_settings`
--
DROP TABLE IF EXISTS `gallery_settings`;
CREATE TABLE `gallery_settings` (
  `id` TEXT NOT NULL,
  `gallery_id` INTEGER NOT NULL,
  `value` TEXT NULL,
  FOREIGN KEY (`gallery_id`) REFERENCES `galleries` (`id`) 
);