import unittest

from Tools import update_version


class UpdateVersionTests(unittest.TestCase):
    def test_replace_hold_menu_version_with_escaped_newline(self) -> None:
        content = '  - name: "\\u3060\\u3053\\u3061\\u3066\\nver 1.1.2"\n'
        updated, count = update_version.replace_hold_menu_version(content, "1.1.3-beta.1")
        self.assertEqual(count, 1)
        self.assertIn('ver 1.1.3-beta.1"', updated)

    def test_replace_hold_menu_version_with_literal_newline(self) -> None:
        content = '  - name: "\n\\u3060\\u3053\\u3061\\u3066\nver 1.1.2"\n'
        updated, count = update_version.replace_hold_menu_version(content, "1.1.3-beta.1")
        self.assertEqual(count, 1)
        self.assertIn('ver 1.1.3-beta.1"', updated)

    def test_parse_version(self) -> None:
        self.assertEqual(update_version.parse_version("1.1.3-beta.1"), "1.1.3-beta.1")
        with self.assertRaises(Exception):
            update_version.parse_version("01.1.3")


if __name__ == "__main__":
    unittest.main()
