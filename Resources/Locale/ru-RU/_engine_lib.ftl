# Используется внутренне функцией THE().
zzzz-the = { PROPER($ent) ->
*[false] the { $ent }
[true] { $ent }
    }

# Используется внутренне функцией SUBJECT().
zzzz-subject-pronoun = { GENDER($ent) ->
[male] он
[female] она
[epicene] они
*[neuter] оно
   }

# Используется внутренне функцией OBJECT().
zzzz-object-pronoun = { GENDER($ent) ->
[male] его
[female] её
[epicene] их
*[neuter] его
   }

# Используется внутренне функцией DAT-OBJ().
# Не используется в en-US. Создано для поддержки других языков.
# (например, «ему», «ей»)
zzzz-dat-object = { GENDER($ent) ->
[male] ему
[female] ей
[epicene] им
*[neuter] ему
   }

# Используется внутренне функцией GENITIVE().
# Не используется в en-US. Создано для поддержки других языков.
# Например: «у него» (русский), «seines Vaters» (немецкий).
zzzz-genitive = { GENDER($ent) ->
[male] его
[female] её
[epicene] их
*[neuter] его
   }

# Используется внутренне функцией POSS-PRONOUN().
zzzz-possessive-pronoun = { GENDER($ent) ->
[male] его
[female] её
[epicene] их
*[neuter] его
   }

# Используется внутренне функцией POSS-ADJ().
zzzz-possessive-adjective = { GENDER($ent) ->
[male] его
[female] её
[epicene] их
*[neuter] его
   }

# Используется внутренне функцией REFLEXIVE().
zzzz-reflexive-pronoun = { GENDER($ent) ->
[male] себя
[female] себя
[epicene] себя
*[neuter] себя
   }

# Используется внутренне функцией CONJUGATE-BE().
zzzz-conjugate-be = { GENDER($ent) ->
[epicene] есть
*[other] есть
   }

# Используется внутренне функцией CONJUGATE-HAVE().
zzzz-conjugate-have = { GENDER($ent) ->
[epicene] имеют
*[other] имеет
   }

# Используется внутренне функцией CONJUGATE-BASIC().
zzzz-conjugate-basic = { GENDER($ent) ->
[epicene] { $first }
*[other] { $second }
   }