extends OptionButton

func _ready():
    add_item("English", 0)
    add_item("Español", 1)

    item_selected.connect(_on_language_selected)
    _sync_with_current_locale()

func _on_language_selected(index):
    match index:
        0:
            TranslationServer.set_locale("en")
        1:
            TranslationServer.set_locale("es")

func _sync_with_current_locale():
    var current_locale = TranslationServer.get_locale()

    match current_locale:
        "en":
            select(0)
        "es":
            select(1)