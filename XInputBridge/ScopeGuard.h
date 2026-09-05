#pragma once

#include <string_view>
#include <utility>

//
// Drop-in for absl::Cleanup using the copy-init form:
//   ScopeCleanup name = [captures] { ... };
//
template <typename F>
class ScopeCleanup
{
public:
	ScopeCleanup(F callback) : m_callback(std::move(callback)) {}
	ScopeCleanup(const ScopeCleanup&) = delete;
	ScopeCleanup& operator=(const ScopeCleanup&) = delete;
	~ScopeCleanup() { m_callback(); }

private:
	F m_callback;
};

template <typename F>
ScopeCleanup(F) -> ScopeCleanup<F>;

//
// ASCII-only, locale-independent case-insensitive compare (absl::EqualsIgnoreCase).
//
inline bool EqualsIgnoreCase(std::string_view left, std::string_view right)
{
	if (left.size() != right.size())
		return false;

	for (size_t i = 0; i < left.size(); ++i)
	{
		unsigned char a = static_cast<unsigned char>(left[i]);
		unsigned char b = static_cast<unsigned char>(right[i]);
		if (a >= 'A' && a <= 'Z')
			a = static_cast<unsigned char>(a + ('a' - 'A'));
		if (b >= 'A' && b <= 'Z')
			b = static_cast<unsigned char>(b + ('a' - 'A'));
		if (a != b)
			return false;
	}

	return true;
}
