--
-- Table structure for table `gallery_settings`
--
DROP TABLE IF EXISTS `gallery_settings`;
CREATE TABLE `gallery_settings` (
  `id` VARCHAR(50) NOT NULL,
  `gallery_id` BIGINT NOT NULL,
  `value` VARCHAR(100) NULL,
  FOREIGN KEY (`gallery_id`) REFERENCES `galleries` (`id`) 
);