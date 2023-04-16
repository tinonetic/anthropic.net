// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preferred naming convention.", Scope = "member", Target = "~F:Anthropic.Net.ApiClient._httpClient")]
[assembly: SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Preferred qualification", Scope = "member", Target = "~M:Anthropic.Net.ApiClient.#ctor(System.Net.Http.HttpClient)")]
[assembly: SuppressMessage("Style", "IDE0021:Use expression body for constructor", Justification = "Preferred convention.", Scope = "member", Target = "~M:Anthropic.Net.ApiClient.#ctor(System.Net.Http.HttpClient)")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this", Justification = "Preferred convention", Scope = "member", Target = "~M:Anthropic.Net.ApiClient.#ctor(System.Net.Http.HttpClient)")]
[assembly: SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Preferred convention.", Scope = "member", Target = "~M:Anthropic.Net.ApiClient.SendRequestAsync(System.String,System.String,System.Collections.Generic.Dictionary{System.String,System.Object})~System.Threading.Tasks.Task{System.Net.Http.HttpResponseMessage}")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this", Justification = "Preferred convention", Scope = "member", Target = "~M:Anthropic.Net.ApiClient.SendRequestAsync(System.String,System.String,System.Collections.Generic.Dictionary{System.String,System.Object})~System.Threading.Tasks.Task{System.Net.Http.HttpResponseMessage}")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:Field names should not begin with underscore", Justification = "Preferred convention.", Scope = "member", Target = "~F:Anthropic.Net.ApiClient._httpClient")]
[assembly: SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Preferred convention", Scope = "member", Target = "~M:Anthropic.Net.ApiClient.CompletionAsync(System.Collections.Generic.Dictionary{System.String,System.Object})~System.Threading.Tasks.Task{System.Text.Json.JsonElement}")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this", Justification = "Preferred convention", Scope = "member", Target = "~M:Anthropic.Net.ApiClient.CompletionAsync(System.Collections.Generic.Dictionary{System.String,System.Object})~System.Threading.Tasks.Task{System.Text.Json.JsonElement}")]
