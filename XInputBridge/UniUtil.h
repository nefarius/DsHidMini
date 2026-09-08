#pragma once

#include <string>

inline std::string ConvertWideToANSI(const std::wstring& wstr)
{
	if (wstr.empty())
		return {};

	const int count = WideCharToMultiByte(
		CP_ACP,
		0,
		wstr.c_str(),
		static_cast<int>(wstr.length()),
		nullptr,
		0,
		nullptr,
		nullptr);

	if (count <= 0)
		return {};

	std::string str(static_cast<size_t>(count), '\0');
	WideCharToMultiByte(
		CP_ACP,
		0,
		wstr.c_str(),
		static_cast<int>(wstr.length()),
		str.data(),
		count,
		nullptr,
		nullptr);
	return str;
}

inline std::wstring ConvertAnsiToWide(const std::string& str)
{
	if (str.empty())
		return {};

	const int count = MultiByteToWideChar(
		CP_ACP,
		0,
		str.c_str(),
		static_cast<int>(str.length()),
		nullptr,
		0);

	if (count <= 0)
		return {};

	std::wstring wstr(static_cast<size_t>(count), L'\0');
	MultiByteToWideChar(
		CP_ACP,
		0,
		str.c_str(),
		static_cast<int>(str.length()),
		wstr.data(),
		count);
	return wstr;
}
