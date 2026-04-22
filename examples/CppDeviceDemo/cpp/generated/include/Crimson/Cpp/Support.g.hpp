#pragma once

#include <array>
#include <cstddef>
#include <cstdint>
#include <map>
#include <memory>
#include <optional>
#include <set>
#include <string>
#include <string_view>
#include <vector>

namespace Crimson::Cpp
{

using String = std::string;
using StringView = std::string_view;

template <typename T>
using Optional = std::optional<T>;

template <typename T>
using List = std::vector<T>;

template <typename T>
using Set = std::set<T>;

template <typename T, std::size_t Length>
using Array = std::array<T, Length>;

template <typename TKey, typename TValue>
using Map = std::map<TKey, TValue>;

template <typename T>
using InterfaceHandle = std::shared_ptr<T>;

}
