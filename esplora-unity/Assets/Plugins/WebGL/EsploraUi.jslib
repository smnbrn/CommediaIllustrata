// Ponte verso la pagina: Unity manda lo stato (JSON) all'interfaccia HTML di esplora/index.html.
mergeInto(LibraryManager.library, {
  EsploraUiAggiorna: function (json) {
    if (typeof window !== "undefined" && window.esploraUi) window.esploraUi(UTF8ToString(json));
  }
});
