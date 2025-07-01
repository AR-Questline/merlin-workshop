extern "C"
{
    const char* getPreferredLanguage()
    {
        NSString* language = [[NSLocale preferredLanguages]firstObject];
        if (language == NULL)
            return NULL;
        
        const char* languageCode = [language UTF8String];
        const size_t len = strlen(languageCode) + 1;
        
        // IL2CPP will free this malloc for us so there wont be a memory leak.
        char* pl = (char*)malloc(len);
                
        strlcpy(pl, languageCode, len);
        return pl;
    }
}
